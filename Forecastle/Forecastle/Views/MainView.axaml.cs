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
                    var pmn = new PodMapNode();
                    pmn.DataContext = new PodInfo(n.Name, "test", n.X, n.Y);
                    ResourceMap2.Children.Add(pmn);
                }
            }
        };
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ((MainViewModel)DataContext).SelectNamespace((e.AddedItems[0] as NamespaceInfo).Namespace);
    }

    public void TryOpenPod(string pod, string ns)
    {
        try
        {
            var vm = ((MainViewModel)DataContext);
            var window = new LogWindow();
            window.DataContext = new LogWindowViewModel(pod, ns,vm);
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
        var mv = (sender as MovableBorder).Parent.DataContext as MapNode;
        if (e == null)
        {
            //TryOpenPod(mv.Name, mv.Namespace);
            return;
        }
        ((MainViewModel)DataContext).MapNodes = ((MainViewModel)DataContext).MapNodes.Select(af =>
        {
            if (af.Id != mv.Id) return af;
            return af with { X = af.X + e.X, Y = af.Y + e.Y };
        }).ToList();
        //Console.WriteLine(mv.ToString());
    }

    private void RecalculateVisiblePods(object? sender, RoutedEventArgs e)
    {
        var nsn = ((sender as CheckBox).Content as string);
        var c = ((sender as CheckBox).IsChecked);
        ((MainViewModel)DataContext).RecalculateVisiblePods(nsn, c ?? false);
    }
}