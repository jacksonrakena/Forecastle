using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Forecastle.ViewModels;

namespace Forecastle.Controls;

public partial class DeploymentMapNode : UserControl
{
    public DeploymentMapNode()
    {
        InitializeComponent();
    }

    private void OnMove(object? sender, TranslateTransform e)
    {
        ((DeploymentNode)this.DataContext).Owner.OnTryMoveNode((MapNode)this.DataContext, e);
    }
}