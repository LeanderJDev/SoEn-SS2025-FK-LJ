using Godot;
using System;

public partial class TurntableControl : Node2D
{
	[Export] public AudioManager AudioManager;

	private Node2D needle;
	private Node2D record;

	private volatile bool _isLeftHolding = false;
	private bool _isRightHolding = false;
	private bool _leftMoved = false;
	private float _lastLoop = 0;

	private bool _rightDragPreviousMotorState = false;

	private float _lastDragAngle = 0f;
	Vector2 recordCenter;

	public override void _Ready()
	{
		needle = GetNode<Polygon2D>("Needle");
		record = GetNode<Sprite2D>("Record");

		recordCenter = record.GlobalPosition;

		if (AudioManager == null)
			return;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton btn)
		{
			if (btn.ButtonIndex == MouseButton.Left && btn.Pressed && !_isLeftHolding)
			{
				_isLeftHolding = true;
				_leftMoved = false;
			}
			if (btn.ButtonIndex == MouseButton.Left && !btn.Pressed)
			{
				_isLeftHolding = false;
				if (!_leftMoved)
				{
					AudioManager.turntable.ToggleMotor();
				}
			}
			if (btn.ButtonIndex == MouseButton.Right && btn.Pressed && !_isRightHolding)
			{
				_isRightHolding = true;
				_rightDragPreviousMotorState = AudioManager.turntable.motorRunning;
				AudioManager.turntable.SetMotorState(false);
				Vector2 mousePos = GetViewport().GetMousePosition();
				Vector2 center = record.GlobalPosition;
				_lastDragAngle = (mousePos - center).Angle();
			}
			if (btn.ButtonIndex == MouseButton.Right && !btn.Pressed)
			{
				_isRightHolding = false;
				if (_rightDragPreviousMotorState)
				{
					AudioManager.turntable.SetMotorState(true);
					AudioManager.turntable.currentSpeed += AudioManager.turntable.targetSpeed*0.3f; // Macht mehr Spaß wenn der Motor nicht von 0 startet
				}
				GD.Print("Drag stopped\n\n");
			}
		}
		if (Input.IsActionPressed("ui_up"))
		{
			AudioManager.turntable.ChangeMotorSpeed(1);
		}
		if (Input.IsActionPressed("ui_down"))
		{
			AudioManager.turntable.ChangeMotorSpeed(-1);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		QueueRedraw();
		if (_isRightHolding)
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			float currentAngle = (mousePos - recordCenter).Angle();
			float angleDelta = Mathf.Wrap(currentAngle - _lastDragAngle, -Mathf.Pi, Mathf.Pi);
			float scratchSpeed = angleDelta / (2 * Mathf.Pi) / (float)delta;

			_lastDragAngle = currentAngle;

			AudioManager.turntable.Scratch(angleDelta / (2 * Mathf.Pi), scratchSpeed);
			if (Mathf.Abs(scratchSpeed) > 0.001f) GD.Print(scratchSpeed);
		}

		if (_isLeftHolding)
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			float localMousePos = mousePos.X - Position.X;
			if (localMousePos > 60 && localMousePos < 185)
			{
				AudioManager.turntable.MoveArm(1 - (localMousePos - 60) / 125);
			}
			if (Math.Abs(AudioManager.turntable.loop - _lastLoop) > 0.5f)
				_leftMoved = true;
		}

		_lastLoop = AudioManager.turntable.loop;
	}

	private Font _defaultFont = ThemeDB.FallbackFont;
	public override void _Draw()
	{
		record.Rotation = AudioManager.turntable.loop % 1 * Mathf.Pi * 2;
		needle.Position = new Vector2((1 - (AudioManager.turntable.loop / AudioManager.turntable.maxLoops)) * 125 + 60, -12);
		// Text für Sample-Länge und aktuellen Index zeichnen
		string info = $"Max Loop: {AudioManager.turntable.maxLoops} | Loop: {AudioManager.turntable.loop:F7} | Speed: {AudioManager.turntable.currentSpeed:F7} | Target Speed: {AudioManager.turntable.targetSpeed:F3}";
		DrawString(_defaultFont, new Vector2(-240, 240), info, HorizontalAlignment.Center);
	}
}
