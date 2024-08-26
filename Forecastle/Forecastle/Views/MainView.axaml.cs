using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Forecastle.Controls;
using Forecastle.ViewModels;
using k8s.Models;

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
                foreach (var pvc in ((MainViewModel)DataContext).MapNodes.OfType<PersistentVolumeClaimNode>())
                {
                    ResourceMap2.Children.Add(new PersistentVolumeClaimMapNode
                    {
                        DataContext = pvc
                    });
                }
                
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
                            break;
                        case PersistentVolumeNode pv:
                            break;
                        case DeploymentNode dp:
                            var node = new DeploymentMapNode
                            {
                                DataContext = dp
                            };
                            // foreach (var binding in dp.GetPvcDependencies())
                            // {
                            //     var pvc = ResourceMap2.Children
                            //         .Where(e => e.DataContext is PersistentVolumeClaimNode pvcn && pvcn.PersistentVolumeClaim.Name() == binding)
                            //         .FirstOrDefault();
                            //     if (pvc == null)
                            //     {
                            //         Console.WriteLine("could not find pvc node for " + binding);
                            //         continue;
                            //     }
                            //     
                            //     var dc = pvc!.DataContext as PersistentVolumeClaimNode;
                            //     ResourceMap2.Children.Add(new Polyline
                            //     {
                            //         Points = new List<Point> { new Point(dc.X+pvc.Bounds.Width, dc.Y+(pvc.Bounds.Height/2)), new Point(dp.X, dp.Y) },
                            //         [Canvas.TopProperty] = 0,
                            //         [Canvas.LeftProperty] = 0,
                            //         Stroke = Brushes.Black
                            //     });
                            // }
                            
                            ResourceMap2.Children.Add(node);
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
    void LoadDependencies(object? sender, RoutedEventArgs e)
    {
        foreach (var d in ResourceMap2.Children.OfType<DeploymentMapNode>().ToList())
        {
            foreach (var binding in (d.DataContext as DeploymentNode).GetPvcDependencies())
            {
                var pvc = ResourceMap2.Children
                    .Where(e => e.DataContext is PersistentVolumeClaimNode pvcn && pvcn.PersistentVolumeClaim.Name() == binding)
                    .FirstOrDefault();
                
                if (pvc == null)
                {
                    Console.WriteLine("could not find pvc node for " + binding);
                    continue;
                }
                                
                var dc = pvc!.DataContext as PersistentVolumeClaimNode;
                ResourceMap2.Children.Add(new Polyline
                {
                    Points = new List<Point> { new Point(dc.X+pvc.Bounds.Width, dc.Y+(pvc.Bounds.Height/2)), new Point(d.Bounds.X, d.Bounds.Y+(d.Bounds.Height/2)) },
                    [Canvas.TopProperty] = 0,
                    [Canvas.LeftProperty] = 0,
                    Stroke = Brushes.Black
                });
            }
        }
    }
}