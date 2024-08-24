using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Forecastle.ViewModels;

namespace Forecastle.Controls;

public partial class PodMapNode : UserControl
{
    public PodMapNode()
    {
        InitializeComponent();
    }

    private void OnMove(object? sender, TranslateTransform e)
    {
        
    }
}