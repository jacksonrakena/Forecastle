using System;
using System.Collections.Generic;
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
public record NamespaceInfo(string Namespace, int DeploymentCount, int PodCount, double X, double Y);

public record PodInfo(string Name, string Namespace, double X, double Y);

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Jackson's Very Experimental Kubernetes Manager";
    [ObservableProperty] private IList<NamespaceInfo> _namespaces = new List<NamespaceInfo>();
    [ObservableProperty] private string _selectedNamespace;
    [ObservableProperty] private IList<PodInfo> _pods = new List<PodInfo>();
    [ObservableProperty] private Kubernetes _kubernetes;
    
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
            var allpods = await Kubernetes.ListPodForAllNamespacesAsync();
            Console.WriteLine($"Retrieved {allpods.Items.Count} pods");
            Pods = allpods.Items.Select(e => new PodInfo(e.Name(), e.Namespace(), r.Next(0,500), r.Next(0,500))).ToList();
            //var namespaces = await Kubernetes.CoreV1.ListNamespaceAsync().ConfigureAwait(false);
            //Console.WriteLine($"Listed {namespaces.Items.Count} namespaces");
            //var nsDetails = new List<NamespaceInfo>();
            /*foreach (var i in namespaces.Items)
            {
                var pods = await Kubernetes.CoreV1.ListNamespacedPodWithHttpMessagesAsync(i.Name()).ConfigureAwait(false);
                var depl = await Kubernetes.ListNamespacedDeploymentAsync(i.Name()).ConfigureAwait(false);
                nsDetails.Add(new NamespaceInfo(i.Name(), depl.Items.Count, pods.Body.Items.Count, r.Next(0,300), r.Next(0,300) ));
            }

            Namespaces = nsDetails;*/
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