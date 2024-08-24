using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Forecastle.ViewModels;

namespace Forecastle.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        ((MainViewModel)DataContext).InitKubernetes();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ((MainViewModel)DataContext).SelectNamespace(e.AddedItems[0].ToString());
    }

    private void OnSelectPod(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            var vm = ((MainViewModel)DataContext);
            var window = new LogWindow();
            window.DataContext = new LogWindowViewModel(e.AddedItems[0].ToString(), vm.SelectedNamespace,vm);
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