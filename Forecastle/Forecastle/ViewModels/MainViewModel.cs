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
public record MapNode(Guid Id, string Namespace, double X, double Y, MainViewModel Owner);
public record PodNode(Guid Id, string Name, string Namespace, double X, double Y, MainViewModel Owner) : MapNode(Id, Namespace, X, Y, Owner);

public record DeploymentNode(Guid Id, string Name, string Namespace, V1Deployment Deployment, double X, double Y, MainViewModel Owner) : MapNode(Id, Namespace, X, Y, Owner);

public record PersistentVolumeClaimNode(
    Guid Id,
    string Name,
    string Namespace,
    V1PersistentVolumeClaim PersistentVolumeClaim,
    double X,
    double Y,
    MainViewModel Owner) : MapNode(Id, Namespace, X, Y, Owner);

public record PersistentVolumeNode(
    Guid Id,
    string Name,
    string Namespace,
    V1PersistentVolume PersistentVolume,
    double X,
    double Y,
    MainViewModel Owner) : MapNode(Id, Namespace, X, Y, Owner);

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Jackson's Very Experimental Kubernetes Manager";
    [ObservableProperty] List<NamespaceInfo> _namespaces = new List<NamespaceInfo>();
    [ObservableProperty] private string _selectedNamespace;
    public Kubernetes Kubernetes;
    [ObservableProperty]
    IEnumerable<MapNode> _mapNodes = Enumerable.Empty<MapNode>();
    List<MapNode> _allnodes = new List<MapNode>();
    
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

    public void RecalculateNodes((string Nst, bool Selected)? preselectPair = null)
    {
        var selectedN = Namespaces
            .Where(e =>
            {
                if (preselectPair == null) return e.Selected;
                return e.Namespace == preselectPair.Value.Nst ? preselectPair.Value.Selected : e.Selected;
            })
            .Select(e => e.Namespace)
            .ToList();

        MapNodes = _allnodes.Where(e => selectedN.Contains(e.Namespace)).ToList();
    }

    public void OnTryMoveNode(MapNode caller, TranslateTransform? transform)
    {
        if (transform == null)
        {
            OnNodeClicked(caller);
            return;
        }
        _allnodes = _allnodes.Select(p =>
        {
            if (p.Id == caller.Id) return p with { X = p.X + transform.X, Y = p.Y + transform.Y };
            return p;
        }).ToList();
        RecalculateNodes();
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

            try
            {
                var pvcs = (await Kubernetes.ListPersistentVolumeClaimForAllNamespacesAsync()).Items.Select(e =>
                    new PersistentVolumeClaimNode(Guid.NewGuid(), e.Name(), e.Namespace(), e, r.Next(0, 500), r.Next(0, 500), this)).ToList();

                _allnodes.AddRange(pvcs);

                var pvs = (await Kubernetes.ListPersistentVolumeAsync()).Items.Select(e =>
                    new PersistentVolumeNode(Guid.NewGuid(), e.Name(), e.Namespace(), e, r.Next(0, 500), r.Next(0, 500), this)).ToList();
                _allnodes.AddRange(pvs);
                
                var dpl = await Kubernetes.ListDeploymentForAllNamespacesAsync();
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
                _allnodes.AddRange(dpl.Items.Select(d => new DeploymentNode(Guid.NewGuid(), d.Name(), d.Namespace(), d, r.Next(0,500), r.Next(0,500), this)).ToList());

                var allpods = (await Kubernetes.ListPodForAllNamespacesAsync())
                    .Items
                    .Select(e => new PodNode(Guid.NewGuid(), e.Name(), e.Namespace(), r.Next(0, 500), r.Next(0, 500), this))
                    .ToList();
                
                Console.WriteLine($"Retrieved {allpods.Count} pods");
                _allnodes.AddRange(allpods);

                var namespaces = (await Kubernetes.CoreV1.ListNamespaceAsync().ConfigureAwait(false))
                    .Items.Select(e => new NamespaceInfo { Namespace = e.Name() })
                    .ToList();
                
                Console.WriteLine($"Listed {namespaces.Count} namespaces");


                Namespaces = namespaces.ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine("Kubernetes failure: ");
                Console.WriteLine(e);
            }
        });
    }
}