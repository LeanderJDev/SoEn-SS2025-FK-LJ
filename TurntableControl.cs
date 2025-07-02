using Godot;
using System;

public partial class TurntableControl : Node2D
{
    [Export] public AudioManager AudioManager;

    private Vector2 _lastMousePos;
    private int _rightClickStartIndex;
    private bool _isLeftHolding = false;
    private bool _isRightHolding = false;
    private bool _isPlaying = true;
    private bool _leftMoved = false;

    public override void _Input(InputEvent @event)
    {
        if (AudioManager == null)
            return;

        if (@event is InputEventMouseButton btn)
        {
            if (btn.ButtonIndex == MouseButton.Left && btn.Pressed && !_isLeftHolding)
            {
                _isLeftHolding = true;
                _lastMousePos = btn.Position;
                _leftMoved = false;
            }
            if (btn.ButtonIndex == MouseButton.Left && !btn.Pressed)
            {
                _isLeftHolding = false;
                AudioManager.SetSpeed(1.0f);
                if (!_leftMoved)
                {
                    _isPlaying = !_isPlaying;
                    if (_isPlaying)
                        AudioManager.Play();
                    else
                        AudioManager.Pause();
                }
            }
            if (btn.ButtonIndex == MouseButton.Right && btn.Pressed && !_isRightHolding)
            {
                _isRightHolding = true;
                _rightClickStartIndex = AudioManager.GetSampleIndex();
                AudioManager.SetSpeed(0);
            }
            if (btn.ButtonIndex == MouseButton.Right && !btn.Pressed)
            {
                _isRightHolding = false;
                AudioManager.SetSpeed(1.0f);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_isLeftHolding)
        {
            Vector2 mousePos = GetViewport().GetMousePosition();
            float percent = mousePos.X / GetViewportRect().Size.X;
            int newIndex = (int)(percent * AudioManager.SampleLength);
            AudioManager.SetSampleIndex(newIndex);
            if ((mousePos - _lastMousePos).Length() > 1.0f)
                _leftMoved = true;
            _lastMousePos = mousePos;
        }

        if (_isRightHolding)
        {
            Vector2 mousePos = GetViewport().GetMousePosition();
            float windowCenter = GetViewportRect().Size.X / 2.0f;
            float dist = mousePos.X - windowCenter;
            AudioManager.SetSampleIndex(_rightClickStartIndex + (int)(dist * 10));
        }
    }
}
