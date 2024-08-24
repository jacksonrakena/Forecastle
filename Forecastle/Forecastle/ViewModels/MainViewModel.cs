using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
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

public interface Transformable
{
    public double X { get; }
    public double Y { get; }
}

public enum MapNodeType { Namespace, Deployment, Pod, PersistentVolumeClaim, PersistentVolume }
public record MapNode(Guid Id, string Name, MapNodeType mnt, double X, double Y);
public record PodInfo(string Name, string Namespace, double X, double Y) : Transformable;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Jackson's Very Experimental Kubernetes Manager";
    [ObservableProperty] private ObservableCollection<NamespaceInfo> _namespaces = new ObservableCollection<NamespaceInfo>();
    [ObservableProperty] private string _selectedNamespace;
    [ObservableProperty] private IList<PodInfo> _pods = new List<PodInfo>();
    [ObservableProperty] private Kubernetes _kubernetes; 
    [ObservableProperty] private IList<MapNode> _mapNodes = new List<MapNode>();
    private IList<PodInfo> _allpods = new List<PodInfo>();
    private IList<V1PersistentVolumeClaim> _allpvcs = new List<V1PersistentVolumeClaim>();
    private IList<V1PersistentVolume> _allpvs = new List<V1PersistentVolume>();
    private IList<V1Deployment> _alldpl = new List<V1Deployment>();
    
    public void RecalculateVisiblePods(string nst, bool selected)
    {
        var r = new Random();
        var selectedN = Namespaces.Where(e => e.Selected).Select(e => e.Namespace).ToList();
        if (selected && !selectedN.Contains(nst)) selectedN.Add(nst);
        if (!selected && selectedN.Contains(nst)) selectedN.Remove(nst);
        Console.WriteLine(string.Join(",",selectedN));
        var mn = new List<MapNode>();
        mn.AddRange(_allpods.Where(e => selectedN.Contains(e.Namespace)).Select(e=>new MapNode(Guid.NewGuid(), e.Name, MapNodeType.Pod, e.X, e.Y)));
        mn.AddRange(_allpvcs.Where(e => selectedN.Contains(e.Namespace())).Select(p=>new MapNode(Guid.NewGuid(), p.Name(), MapNodeType.PersistentVolumeClaim, r.Next(0,500),r.Next(0,500) )));
        mn.AddRange(_allpvs.Where(e => selectedN.Contains(e.Namespace())).Select(rx => new MapNode(Guid.NewGuid(), rx.Name(), MapNodeType.PersistentVolume, r.Next(0,500),r.Next(0,500))));
        mn.AddRange(_alldpl.Where(e => selectedN.Contains(e.Name())).Select(rx => new MapNode(Guid.NewGuid(), rx.Name(), MapNodeType.Deployment, r.Next(0,500),r.Next(0,500))));
        MapNodes = mn;
        //Pods = _allpods.Where(e => selectedN.Contains(e.Namespace)).ToList();
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
            _allpvcs = pvcs.Items;
            Console.WriteLine($"Retrieved {pvcs.Items.Count} pvcs");

            var pvs = await Kubernetes.ListPersistentVolumeAsync();
            _allpvs = pvs.Items;
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
            _allpods = allpods.Items.Select(e => new PodInfo(e.Name(), e.Namespace(), r.Next(0,500), r.Next(0,500))).ToList();
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