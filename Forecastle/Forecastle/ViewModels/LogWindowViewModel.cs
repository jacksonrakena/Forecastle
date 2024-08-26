using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using k8s;
using k8s.Models;

namespace Forecastle.ViewModels;

public class LogWindowTab: INotifyPropertyChanged
{
    public string Header { get; }
    public StringBuilder Content { get; }
    public LogWindowTab(string header, StringBuilder content)
    {
        Header = header;
        Content = content;
    }
    public void AppendLine(string toAppend)
    {
        Content.AppendLine(toAppend);
        OnPropertyChanged(nameof(Content));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
public partial class LogWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _podName;
    [ObservableProperty] private StringBuilder _log;
    [ObservableProperty] private string _ns;
    [ObservableProperty] private MainViewModel _viewModel;
    [ObservableProperty] List<LogWindowTab> _containers = new();
    List<CancellationTokenSource> _cts = new List<CancellationTokenSource>();
    public LogWindowViewModel(PodNode pod, MainViewModel viewModel)
    {
        ViewModel = viewModel;
        PodName = pod.Pod.Name();
        Ns = pod.Pod.Namespace();
        Log = new StringBuilder();

        foreach (var container in pod.Pod.Spec.Containers)
        {
            var lwt = new LogWindowTab(container.Name, new StringBuilder());
            Containers.Add(lwt);
            var ct = new CancellationTokenSource();
            _cts.Add(ct);
            Task.Run(async () =>
            {
                try
                {
                    var s = await viewModel.Kubernetes.CoreV1.ReadNamespacedPodLogAsync(pod.Pod.Name(), pod.Pod.Namespace(), container: container.Name, follow:true);
                    Console.WriteLine($"Loaded stream for {pod.Pod.Namespace()}/{pod.Pod.Name()}/{container.Name}");
                    if (s == null)
                    {
                        Console.WriteLine("nullstream");
                        return;
                    }

                    var sr = new StreamReader(s);
                    var blockx = MemoryPool<char>.Shared.Rent(1);
                    var block = blockx.Memory;
                    try
                    {
                        while (!ct.IsCancellationRequested)
                        {
                             var vr=await sr.ReadLineAsync(ct.Token);
                            //await sr.ReadAsync(block, ct.Token);
                            //lwt.Content.Append(block);
                            lwt.AppendLine(vr);
                        }
                    }
                    catch (TaskCanceledException tce)
                    {
                        Console.WriteLine($"{pod.Pod.Namespace()}/{pod.Pod.Name()}/{container.Name}: Destroying stream");
                        s.Dispose();
                        blockx.Dispose();   
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }, ct.Token);
        }
    }
    
    public void Destroy()
    {
        foreach (var ct in _cts)
        {
            ct.Cancel();
        }
    }
}