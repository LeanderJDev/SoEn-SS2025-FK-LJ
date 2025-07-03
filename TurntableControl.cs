using Godot;
using System;
using System.Data;

public partial class TurntableControl : Node2D
{
    [Export] public AudioManager AudioManager;

    private Node2D needle;
    private Node2D record;

    private float _rightClickStartIndex;
    private bool _isLeftHolding = false;
    private bool _isRightHolding = false;
    private bool _leftMoved = false;

    private float maxLoops = 670;
    private float loop = 0;
    private float _lastLoop = 0;

    private bool motorRunning = true;
    private const float motorSpeed = 45f;

    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private const float acceleration = 1.0f; // Umdrehungen pro Sekunde^2, anpassen nach Gefühl
    private const float drag = 0.5f;
    private float rightDragLastY = 0f;
    private Vector2 rightDragLastMousePos;
    private float rightDragLastLoop;
    private float rightDragLastSpeed;
    private Vector2 _rightDragLastMousePos = Vector2.Zero;
    private float _rightDragLastLoop = 0f;

    public override void _Ready()
    {
        needle = GetNode<Polygon2D>("Needle");
        record = GetNode<Sprite2D>("Record");

        if (AudioManager == null)
            return;

        maxLoops = motorSpeed/60 * AudioManager.SampleLength / 44100;
    }

    public override void _Input(InputEvent @event)
    {
        if (AudioManager == null)
            return;

        if (@event is InputEventMouseButton btn)
        {
            if (btn.ButtonIndex == MouseButton.Left && btn.Pressed && !_isLeftHolding)
            {
                _isLeftHolding = true;
                _leftMoved = false;
                targetSpeed = 0f; // Sofort abbremsen
            }
            if (btn.ButtonIndex == MouseButton.Left && !btn.Pressed)
            {
                _isLeftHolding = false;
                if (!_leftMoved)
                {
                    if (!motorRunning)
                        StartMotor();
                    else
                        StopMotor();
                }
                // Zielgeschwindigkeit auf 0 setzen, damit er langsam ausrollt
                targetSpeed = 0f;
            }
            if (btn.ButtonIndex == MouseButton.Right && btn.Pressed && !_isRightHolding)
            {
                _isRightHolding = true;
                _rightClickStartIndex = loop;
                _rightDragLastMousePos = btn.Position;
                _rightDragLastLoop = loop;
            }
            if (btn.ButtonIndex == MouseButton.Right && !btn.Pressed)
            {
                _isRightHolding = false;
                // Nach Loslassen bleibt die aktuelle Geschwindigkeit erhalten (Schwung)
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
        // Drag
        currentSpeed -= currentSpeed * drag * (float)delta;

        // Inertia-Modell: Geschwindigkeit an Zielgeschwindigkeit angleichen
        if (Mathf.Abs(currentSpeed - targetSpeed) > 0.001f)
        {
            float sign = MathF.Sign(targetSpeed - currentSpeed);
            currentSpeed += sign * acceleration * (float)delta;
            // Nicht überschießen
            if (sign != MathF.Sign(targetSpeed - currentSpeed))
                currentSpeed = targetSpeed;
        }

        if (motorRunning && !_isLeftHolding && !_isRightHolding)
        {
            targetSpeed = motorSpeed / 60.0f; // Ziel: normale Motor-Geschwindigkeit
        }

        if (_isRightHolding)
        {
            Vector2 mousePos = GetViewport().GetMousePosition();
            Vector2 center = record.GlobalPosition;
            float lastAngle = (_rightDragLastMousePos - center).Angle();
            float newAngle = (mousePos - center).Angle();
            float angleDelta = Mathf.Wrap(newAngle - lastAngle, -Mathf.Pi, Mathf.Pi);
            float loopDelta = angleDelta / (Mathf.Pi * 2);
            loop += loopDelta;
            currentSpeed = (loop - _rightDragLastLoop) / (float)delta;
            _rightDragLastMousePos = mousePos;
            _rightDragLastLoop = loop;
            QueueRedraw();
        }
        else if (motorRunning || MathF.Abs(currentSpeed) > 0.0001f)
        {
            loop += currentSpeed * (float)delta;
            if (loop >= maxLoops)
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
            if (Math.Abs(loop - _lastLoop) > 0.5f)
                _leftMoved = true;
            // Setze targetSpeed auf 0, damit er beim Loslassen ausrollt
            targetSpeed = 0f;
            QueueRedraw();
        }

        // Geschwindigkeit in Songdurchläufe pro Sekunde umrechnen
        AudioManager.FillBuffer((float)delta, currentSpeed / maxLoops, loop / maxLoops);

        if (Mathf.Abs(currentSpeed) < 0.001f)
            AudioManager.Pause();
        else
            AudioManager.Play();

        _lastLoop = loop;
    }

    private Font _defaultFont = ThemeDB.FallbackFont;
    public override void _Draw()
    {
        record.Rotation = loop % 1 * Mathf.Pi * 2;
        needle.Position = new Vector2((1 - (loop / maxLoops)) * 125 + 60, -12);
        // Text für Sample-Länge und aktuellen Index zeichnen
        string info = $"Max Loop: {maxLoops} | Loop: {loop}";
        DrawString(_defaultFont, new Vector2(-120, 240), info, HorizontalAlignment.Center);
    }
}
