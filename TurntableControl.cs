using Godot;
using System;
using System.Data;

public partial class TurntableControl : Node2D
{
    [Export] public AudioManager AudioManager;

    private Polygon2D needle;
    private Sprite2D record;

    private Vector2 _lastMousePos;
    private float _rightClickStartIndex;
    private bool _isLeftHolding = false;
    private bool _isRightHolding = false;
    private bool _isPlaying = true;
    private bool _leftMoved = false;

    private float maxLoops = 670;

    private float loop = 0;

    private float _lastLoop = 0;

    private bool motorRunning = true;

    private const float motorSpeed = 45f;

    public override void _Ready()
    {
        needle = GetNode<Polygon2D>("Needle");
        record = GetNode<Sprite2D>("Record");
    }

    public override void _Input(InputEvent @event)
    {
        if (AudioManager == null)
            return;
        
        maxLoops = motorSpeed/60 * AudioManager.SampleLength / 44100;

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
                if (!_leftMoved)
                {
                    _isPlaying = !_isPlaying;
                    if (_isPlaying)
                        StartMotor();
                    else
                        StopMotor();
                }
            }
            if (btn.ButtonIndex == MouseButton.Right && btn.Pressed && !_isRightHolding)
            {
                _isRightHolding = true;
                _rightClickStartIndex = loop;
            }
            if (btn.ButtonIndex == MouseButton.Right && !btn.Pressed)
            {
                _isRightHolding = false;
            }
        }
    }

    private void StartMotor()
    {
        motorRunning = true;
        AudioManager.Play();
    }

    private void StopMotor()
    {
        motorRunning = false;
        AudioManager.Pause();
    }

    public override void _Process(double delta)
    {
        if (motorRunning)
        {
            loop += (float)(motorSpeed / 60.0 * delta);
            record.Rotation = loop%1 * Mathf.Pi * 2;
            needle.Position = new Vector2((1 - (loop / maxLoops)) * 125 + 60, -12); // 60 - 185
            if (loop == maxLoops)
            {
                StopMotor();
            }
            QueueRedraw();
        }
        if (_isLeftHolding)
        {
            Vector2 mousePos = GetViewport().GetMousePosition();
            float localMousePos = mousePos.X - Position.X;
            if (localMousePos > 60 && localMousePos < 185)
            {
                float offset = loop % 1;
                loop = (int)((1 - (localMousePos - 60) / 125) * maxLoops);
                loop += offset;
            }
            if ((mousePos - _lastMousePos).Length() > 1.0f)
                _leftMoved = true;
            _lastMousePos = mousePos;
            QueueRedraw();
        }

        if (_isRightHolding)
        {
            Vector2 mousePos = GetViewport().GetMousePosition();
            float windowCenter = GetViewportRect().Size.Y / 2.0f;
            float dist = mousePos.Y - windowCenter;
            loop = _rightClickStartIndex + (dist * 0.002f);
            QueueRedraw();
        }
        AudioManager.SetPosition(loop / maxLoops);
        if(Mathf.Abs(loop - _lastLoop) < 0.001f){
            AudioManager.Pause();
        }
        else{
            AudioManager.Play();
        }
        _lastLoop = loop;
    }

    private Font _defaultFont = ThemeDB.FallbackFont;
    public override void _Draw()
    {
        // Text für Sample-Länge und aktuellen Index zeichnen
        string info = $"Max Loop: {maxLoops} | Loop: {loop}";
        DrawString(_defaultFont, new Vector2(-120, 240), info, HorizontalAlignment.Center);
    }
}
