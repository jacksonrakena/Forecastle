using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Forecastle.Views;
using k8s;
using k8s.Models;

namespace Forecastle.ViewModels;

public partial class NamespaceInfo : ObservableObject
{
    [ObservableProperty] private string _namespace;
    [ObservableProperty] private int _deploymentCount;
    [ObservableProperty] private int _podCount;
    [ObservableProperty] private bool _selected = true;
}
public record MapNode(Guid Id, double X, double Y, MainViewModel Owner);
public record PodNode(Guid Id, string Name, string Namespace, double X, double Y, MainViewModel Owner) : MapNode(Id, X, Y, Owner);

public record PersistentVolumeClaimNode(
    Guid Id,
    string Name,
    V1PersistentVolumeClaim PersistentVolumeClaim,
    double X,
    double Y,
    MainViewModel Owner) : MapNode(Id, X, Y, Owner);

public record PersistentVolumeNode(
    Guid Id,
    string Name,
    V1PersistentVolume PersistentVolume,
    double X,
    double Y,
    MainViewModel Owner) : MapNode(Id, X, Y, Owner);

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Jackson's Very Experimental Kubernetes Manager";
    [ObservableProperty] private ObservableCollection<NamespaceInfo> _namespaces = new ObservableCollection<NamespaceInfo>();
    [ObservableProperty] private string _selectedNamespace;
    [ObservableProperty] private IList<PodNode> _pods = new List<PodNode>();
    [ObservableProperty] private Kubernetes _kubernetes; 
    [ObservableProperty] private IList<MapNode> _mapNodes = new List<MapNode>();
    private IList<PodNode> _allpods = new List<PodNode>();
    private IList<PersistentVolumeClaimNode> _allpvcs = new List<PersistentVolumeClaimNode>();
    private IList<PersistentVolumeNode> _allpvs = new List<PersistentVolumeNode>();
    private IList<V1Deployment> _alldpl = new List<V1Deployment>();
    
    public void RecalculateVisiblePods(string nst, bool selected)
    {
        var r = new Random();
        var selectedN = Namespaces.Where(e => e.Selected).Select(e => e.Namespace).ToList();
        if (selected && !selectedN.Contains(nst)) selectedN.Add(nst);
        if (!selected && selectedN.Contains(nst)) selectedN.Remove(nst);
        Console.WriteLine(string.Join(",",selectedN));
        var mn = new List<MapNode>();
        mn.AddRange(_allpods.Where(e => selectedN.Contains(e.Namespace)));
        mn.AddRange(_allpvcs.Where(e => selectedN.Contains(e.PersistentVolumeClaim.Namespace())));
        //mn.AddRange(_allpvcs.Where(e => selectedN.Contains(e.Namespace())).Select(p=>new MapNode(Guid.NewGuid(), MapNodeType.PersistentVolumeClaim, MapNodeType.PersistentVolumeClaim, r.Next(0,500),r.Next(0,500) )));
        //mn.AddRange(_allpvs.Where(e => selectedN.Contains(e.Namespace())).Select(rx => new MapNode(Guid.NewGuid(), rx.Name(), MapNodeType.PersistentVolume, r.Next(0,500),r.Next(0,500))));
        //mn.AddRange(_alldpl.Where(e => selectedN.Contains(e.Name())).Select(rx => new MapNode(Guid.NewGuid(), rx.Name(), MapNodeType.Deployment, r.Next(0,500),r.Next(0,500))));
        MapNodes = mn;
        //Pods = _allpods.Where(e => selectedN.Contains(e.Namespace)).ToList();
    }

    public void OnNodeClicked(MapNode caller)
    {
        if (caller is PodNode pi)
        {
            try
            {
                var window = new LogWindow();
                window.DataContext = new LogWindowViewModel(pi.Name, pi.Namespace,this);
                Dispatcher.UIThread.Invoke(() =>
                {
                    window.Show(
                        (((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime)
                            .MainWindow));
                });
            }
            catch (Exception ed)
            {
                Console.WriteLine(ed);
            }
        }
    }

    public void OnTryMoveNode(MapNode caller, TranslateTransform? transform)
    {
        if (transform == null)
        {
            OnNodeClicked(caller);
            return;
        }
        MapNodes = MapNodes.Select(p =>
        {
            if (p.Id == caller.Id) return p with { X = p.X + transform.X, Y = p.Y + transform.Y };
            return p;
        }).ToList();
    }
    
    public void InitKubernetes()
    {
        var r = new Random();
        Task.Run(async () =>
        {
            var config = await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(new FileInfo(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? System.IO.Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? @"\", @".kube\config")
                : System.IO.Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "/", ".kube/config")));
            
            Console.WriteLine("Initialised config: " + config.Host);
            Kubernetes = new Kubernetes(config);

            var pvcs = await Kubernetes.ListPersistentVolumeClaimForAllNamespacesAsync();
            _allpvcs = pvcs.Items.Select(e =>
                new PersistentVolumeClaimNode(Guid.NewGuid(), e.Name(), e, r.Next(0, 500), r.Next(0, 500), this)).ToList();
            Console.WriteLine($"Retrieved {pvcs.Items.Count} pvcs");

            var pvs = await Kubernetes.ListPersistentVolumeAsync();
            _allpvs = pvs.Items.Select(e =>
                new PersistentVolumeNode(Guid.NewGuid(), e.Name(), e, r.Next(0, 500), r.Next(0, 500), this)).ToList();
            
            var dpl = await Kubernetes.ListDeploymentForAllNamespacesAsync();
            _alldpl = dpl.Items;
            foreach (var d in dpl.Items)
            {
                foreach (var rc in d.Spec?.Template?.Spec?.Volumes ?? Enumerable.Empty<V1Volume>())
                {
                    try
                    {
                        if (rc.PersistentVolumeClaim != null) 
                            Console.WriteLine($"{d.Name()} => {rc.PersistentVolumeClaim.ClaimName}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    
                }
            }
            
            var allpods = await Kubernetes.ListPodForAllNamespacesAsync();
            Console.WriteLine($"Retrieved {allpods.Items.Count} pods");
            
            var namespaces = await Kubernetes.CoreV1.ListNamespaceAsync().ConfigureAwait(false);
            Console.WriteLine($"Listed {namespaces.Items.Count} namespaces");
            var nsDetails = new List<NamespaceInfo>();
            foreach (var i in namespaces.Items)
            {
                //var pods = await Kubernetes.CoreV1.ListNamespacedPodWithHttpMessagesAsync(i.Name()).ConfigureAwait(false);
                //var depl = await Kubernetes.ListNamespacedDeploymentAsync(i.Name()).ConfigureAwait(false);
                nsDetails.Add(new NamespaceInfo { Namespace = i.Name(), DeploymentCount = 0, PodCount = 0 });
            }

            Namespaces = new ObservableCollection<NamespaceInfo>(nsDetails);
            _allpods = allpods.Items.Select(e => new PodNode(Guid.NewGuid(), e.Name(), e.Namespace(), r.Next(0,500), r.Next(0,500), this)).ToList();
            Pods = _allpods;
        });
    }

    public void SelectNamespace(string ns)
    {
        /*SelectedNamespace = ns;
        Task.Run(async () =>
        {
            var pods = await Kubernetes.CoreV1.ListNamespacedPodWithHttpMessagesAsync(ns).ConfigureAwait(false);
            Console.WriteLine(pods.Body.Items.Count);
            Pods = pods.Body.Items.Select(e => $"{e.Name()}").ToList();
        });*/
    }
}