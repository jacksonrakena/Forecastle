using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Forecastle.ViewModels;

namespace Forecastle.Controls;

public partial class PersistentVolumeClaimMapNode : UserControl
{
    public PersistentVolumeClaimMapNode()
    {
        InitializeComponent();
    }

    private void OnMove(object? sender, TranslateTransform e)
    {
        ((PersistentVolumeClaimNode)this.DataContext).Owner.OnTryMoveNode((MapNode)this.DataContext, e);
    }
}