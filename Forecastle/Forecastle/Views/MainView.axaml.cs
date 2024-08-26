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
                            ResourceMap2.Children.Add(new PodMapNode
                            {
                                DataContext = pi
                            });
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
                        case DeploymentNode dp:
                            ResourceMap2.Children.Add(new DeploymentMapNode
                            {
                                DataContext = dp
                            });
                            break;
                    }
                }
            }
        };
    }

    private void RecalculateVisiblePods(object? sender, RoutedEventArgs e)
    {
        var nsn = ((sender as CheckBox).Content as string);
        var c = ((sender as CheckBox).IsChecked);
        ((MainViewModel)DataContext).RecalculateNodes((nsn, c ?? false));
    }
}