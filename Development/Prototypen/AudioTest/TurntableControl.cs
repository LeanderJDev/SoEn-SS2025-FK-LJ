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
	private volatile float loop = 0;
	private float _lastLoop = 0;

	private Vector2 _rightDragLastMousePos = Vector2.Zero;
	private float _rightDragLastLoop = 0f;

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
				_rightDragLastLoop = loop;
			}
			if (btn.ButtonIndex == MouseButton.Right && !btn.Pressed)
			{
				_isRightHolding = false;
				// Nach Loslassen bleibt die aktuelle Geschwindigkeit erhalten (Schwung)
			}
		}
	}

	public override void _Process(double delta)
	{
		if (Mathf.Abs(AudioManager.turntable.currentSpeed) > 0.001f)
		{
			QueueRedraw();
		}

		loop = AudioManager.turntable.loop;

		if (_isRightHolding)
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			Vector2 center = record.GlobalPosition;
			float lastAngle = (_rightDragLastMousePos - center).Angle();
			float newAngle = (mousePos - center).Angle();
			float angleDelta = Mathf.Wrap(newAngle - lastAngle, -Mathf.Pi, Mathf.Pi);
			float loopDelta = angleDelta / (Mathf.Pi * 2);
			loop += loopDelta;
			AudioManager.turntable.currentSpeed = (loop - _rightDragLastLoop) / (float)delta;
			_rightDragLastMousePos = mousePos;
			_rightDragLastLoop = loop;
			QueueRedraw();
		}

		if (_isLeftHolding)
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			float localMousePos = mousePos.X - Position.X;
			if (localMousePos > 60 && localMousePos < 185)
			{
				float offset = loop % 1;
				loop = (int)((1 - (localMousePos - 60) / 125) * AudioManager.turntable.maxLoops);
				loop += offset;
				AudioManager.JumpTo(loop);
			}
			if (Math.Abs(loop - _lastLoop) > 0.5f)
				_leftMoved = true;
			QueueRedraw();
		}
		_lastLoop = loop;
	}

	private Font _defaultFont = ThemeDB.FallbackFont;
	public override void _Draw()
	{
		record.Rotation = loop % 1 * Mathf.Pi * 2;
		needle.Position = new Vector2((1 - (loop / AudioManager.turntable.maxLoops)) * 125 + 60, -12);
		// Text für Sample-Länge und aktuellen Index zeichnen
		string info = $"Max Loop: {AudioManager.turntable.maxLoops} | Loop: {loop} | Speed: {AudioManager.turntable.currentSpeed} | Target Speed: {AudioManager.turntable.targetSpeed}";
		DrawString(_defaultFont, new Vector2(-240, 240), info, HorizontalAlignment.Center);
	}
}
