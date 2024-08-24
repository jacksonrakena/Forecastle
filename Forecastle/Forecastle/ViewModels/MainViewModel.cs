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

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Jackson's Very Experimental Kubernetes Manager";
    [ObservableProperty] private IList<string> _namespaces = new List<string>();
    [ObservableProperty] private string _selectedNamespace;
    [ObservableProperty] private IList<string> _pods = new List<string>();
    [ObservableProperty] private Kubernetes _kubernetes;

    public void InitKubernetes()
    {
        Task.Run(async () =>
        {
            var config = await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(new FileInfo(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? System.IO.Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? @"\", @".kube\config")
                : System.IO.Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "/", ".kube/config")));
            
            Console.WriteLine("Initialised config: " + config.Host);
            Kubernetes = new Kubernetes(config);
            var namespaces = await Kubernetes.CoreV1.ListNamespaceAsync().ConfigureAwait(false);
            Console.WriteLine($"Listed {namespaces.Items.Count} namespaces");
            Namespaces = namespaces.Items.Select(e => $"{e.Name()}").ToList();
        });
    }

    public void SelectNamespace(string ns)
    {
        SelectedNamespace = ns;
        Task.Run(async () =>
        {
            var pods = await Kubernetes.CoreV1.ListNamespacedPodWithHttpMessagesAsync(ns).ConfigureAwait(false);
            Console.WriteLine(pods.Body.Items.Count);
            Pods = pods.Body.Items.Select(e => $"{e.Name()}").ToList();
        });
    }
}