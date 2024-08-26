using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Forecastle.Controls;

public class MovableBorder : Border
{
    public event EventHandler<TranslateTransform> OnMove;
    private bool _isPressed; 
    private Point _positionInBlock;
    private TranslateTransform _transform = null!;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _isPressed = true;
        _positionInBlock = e.GetPosition(Parent.Parent as Visual);
		
        if (_transform != null!) 
            _positionInBlock = new Point(
                _positionInBlock.X - _transform.X,
                _positionInBlock.Y - _transform.Y);
		
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isPressed = false;
        OnMove?.Invoke(this,(this.Parent as Visual).RenderTransform as TranslateTransform);
        (this.Parent as Visual).RenderTransform = null;
        _positionInBlock = default;
        _transform = null!;
		
        base.OnPointerReleased(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isPressed)
            return;
		
        if (Parent == null)
            return;

        var currentPosition = e.GetPosition(Parent.Parent as Visual);

        var offsetX = currentPosition.X -  _positionInBlock.X;
        var offsetY = currentPosition.Y - _positionInBlock.Y;

        _transform = new TranslateTransform(offsetX, offsetY);
        (this.Parent as Visual).RenderTransform = _transform;
		
        base.OnPointerMoved(e);
    }
}