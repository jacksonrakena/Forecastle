using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using k8s;

namespace Forecastle.ViewModels;

public partial class LogWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _podName;
    [ObservableProperty] private StringBuilder _log;
    [ObservableProperty] private string _ns;
    [ObservableProperty] private MainViewModel _viewModel;
    public LogWindowViewModel(string podName, string ns, MainViewModel viewModel)
    {
        ViewModel = viewModel;
        PodName = podName;
        Ns = ns;
        Log = new StringBuilder();

        Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("Fetching stream");
                var s = await viewModel.Kubernetes.CoreV1.ReadNamespacedPodLogAsync(podName, ns, follow:true);
                if (s == null)
                {
                    Console.WriteLine("nullstream");
                    return;
                }

                var sr = new StreamReader(s);
                var block = MemoryPool<char>.Shared.Rent(1000).Memory;
                while (true)
                {
                    await sr.ReadAsync(block);
                    Log.Append(block);
                    OnPropertyChanged(nameof(Log));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
        
    }
}