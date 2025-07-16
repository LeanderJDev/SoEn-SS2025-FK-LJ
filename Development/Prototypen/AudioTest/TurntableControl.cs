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

	private Vector2 _rightDragLastMousePos = Vector2.Zero;
	private float _rightDragLastLoop = 0f;
	private float _rightDragAngleOffset = 0f;
	private bool _rightDragPreviousMotorState = false;

	public override void _Ready()
	{
		needle = GetNode<Polygon2D>("Needle");
		record = GetNode<Sprite2D>("Record");

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
				_rightDragLastMousePos = btn.Position;
				_rightDragLastLoop = AudioManager.turntable.loop;
				_rightDragPreviousMotorState = AudioManager.turntable.motorRunning;
				AudioManager.turntable.StopMotor();
				AudioManager.turntable.motorRunning = false;
				Vector2 mousePos = GetViewport().GetMousePosition();
				Vector2 center = record.GlobalPosition;
				float lastAngle = (AudioManager.turntable.loop * 2 * Mathf.Pi);
				float newAngle = (mousePos - center).Angle();
				_rightDragAngleOffset = Mathf.Wrap(newAngle - lastAngle, -Mathf.Pi, Mathf.Pi);
			}
			if (btn.ButtonIndex == MouseButton.Right && !btn.Pressed)
			{
				_isRightHolding = false;
				if (_rightDragPreviousMotorState)
				{
					AudioManager.turntable.StartMotor();
					AudioManager.turntable.motorRunning = true;
				}
			}
		}
		if (Input.IsActionPressed("ui_up"))
		{
			AudioManager.turntable.motorSpeed += 1;
			AudioManager.turntable.StartMotor();
		}
		if (Input.IsActionPressed("ui_down"))
		{
			AudioManager.turntable.motorSpeed -= 1;
			AudioManager.turntable.StartMotor();
		}
	}

	public override void _Process(double delta)
	{
		QueueRedraw();

		if (_isRightHolding)
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			Vector2 center = record.GlobalPosition;
			float lastAngle = (AudioManager.turntable.loop * 2 * Mathf.Pi);
			_rightDragLastLoop = AudioManager.turntable.loop;
			float newAngle = (mousePos - center).Angle() - _rightDragAngleOffset;
			float angleDelta = Mathf.Wrap(newAngle - lastAngle, -Mathf.Pi, Mathf.Pi);

			AudioManager.turntable.Rotate(angleDelta);
			AudioManager.turntable.currentSpeed = (angleDelta / (4*Mathf.Pi)) / (float)delta;
			QueueRedraw();
		}

		if (_isLeftHolding)
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			float localMousePos = mousePos.X - Position.X;
			if (localMousePos > 60 && localMousePos < 185)
			{
				float offset = AudioManager.turntable.loop % 1;
				AudioManager.turntable.loop = (int)((1 - (localMousePos - 60) / 125) * AudioManager.turntable.maxLoops);
				AudioManager.turntable.loop += offset;
				AudioManager.JumpTo(AudioManager.turntable.loop);
			}
			if (Math.Abs(AudioManager.turntable.loop - _lastLoop) > 0.5f)
				_leftMoved = true;
			QueueRedraw();
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
