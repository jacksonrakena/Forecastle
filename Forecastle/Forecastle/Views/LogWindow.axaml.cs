using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Forecastle.ViewModels;

namespace Forecastle.Views;

public partial class LogWindow : Window
{
    public LogWindow()
    {
        this.Closing += (sender, args) => ((LogWindowViewModel)DataContext).Destroy();
        InitializeComponent();
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    //Dispatcher.UIThread.Invoke(() => LogScrollable.ScrollToEnd());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                await Task.Delay(1000);   
            }
        });
    }
    public void Init()
    {
        foreach (var v in ((LogWindowViewModel)DataContext).Containers)
        {
        }
    }
}