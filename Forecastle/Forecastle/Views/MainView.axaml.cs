using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Forecastle.Controls;
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
        
        ((INotifyPropertyChanged)DataContext).PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == "MapNodes")
            {
                ResourceMap2.Children.Clear();
                foreach (var n in ((MainViewModel)DataContext).MapNodes)
                {
                    switch (n)
                    {
                        case PodNode pi:
                        {
                            var pmn = new PodMapNode
                            {
                                DataContext = pi
                            };
                            ResourceMap2.Children.Add(pmn);
                            break;
                        }
                        case PersistentVolumeClaimNode pvc:
                            ResourceMap2.Children.Add(new PersistentVolumeClaimMapNode
                            {
                                DataContext = pvc
                            });
                            break;
                        case PersistentVolumeNode pv:
                            break;
                    }
                }
            }
        };
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ((MainViewModel)DataContext).SelectNamespace((e.AddedItems[0] as NamespaceInfo).Namespace);
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

    private void MovableBorder_OnOnMove(object? sender, TranslateTransform e)
    {

    }

    private void RecalculateVisiblePods(object? sender, RoutedEventArgs e)
    {
        var nsn = ((sender as CheckBox).Content as string);
        var c = ((sender as CheckBox).IsChecked);
        ((MainViewModel)DataContext).RecalculateVisiblePods(nsn, c ?? false);
    }
}