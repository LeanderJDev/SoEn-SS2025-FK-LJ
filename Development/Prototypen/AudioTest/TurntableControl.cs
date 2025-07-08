using Godot;
using System;
using System.Data;
using System.Threading;

public partial class TurntableControl : Node2D
{
	[Export] public AudioManager AudioManager;

	private Node2D needle;
	private Node2D record;

	private bool _isLeftHolding = false;
	private bool _isRightHolding = false;
	private bool _leftMoved = false;

	private float maxLoops = 670;
	private volatile float loop = 0;
	private float _lastLoop = 0;

	private bool motorRunning = true;
	private const float motorSpeed = 45f;

	private volatile float currentSpeed = 0f;
	private volatile float targetSpeed = 0f;
	private const float acceleration = 1.0f; // Umdrehungen pro Sekunde^2, anpassen nach Gefühl
	private const float drag = 0.9f;
	private Vector2 _rightDragLastMousePos = Vector2.Zero;
	private float _rightDragLastLoop = 0f;

	private Thread _turntableThread;
	private volatile bool _threadRunning = false;

	public override void _Ready()
	{
		needle = GetNode<Polygon2D>("Needle");
		record = GetNode<Sprite2D>("Record");

		if (AudioManager == null)
			return;

		maxLoops = motorSpeed/60 * AudioManager.SampleLength / 44100;

		_threadRunning = true;
		_turntableThread = new Thread(TurntableThreadLoop);
		_turntableThread.Start();
	}

	public override void _ExitTree()
	{
		_threadRunning = false;
		_turntableThread?.Join();
	}

	private void TurntableThreadLoop()
	{
		var sw = new System.Diagnostics.Stopwatch();
		sw.Start();
		double lastTime = sw.Elapsed.TotalSeconds;
		while (_threadRunning)
		{
			double now = sw.Elapsed.TotalSeconds;
			double delta = now - lastTime;
			lastTime = now;
			// Drag
			currentSpeed -= currentSpeed * drag * (float)delta;
			// Inertia
			if (MathF.Abs(currentSpeed - targetSpeed) > 0.001f)
			{
				float sign = MathF.Sign(targetSpeed - currentSpeed);
				currentSpeed += sign * acceleration * (float)delta;
				// Stabilisieren der Zielgeschwindigkeit
				if (sign != MathF.Sign(targetSpeed - currentSpeed))
					currentSpeed = targetSpeed;
			}
			if (motorRunning)
			{
				targetSpeed = motorSpeed / 60.0f;
			}

			if (MathF.Abs(currentSpeed) > 0.0001f && !_isRightHolding)
			{
				loop += currentSpeed * (float)delta;
				if (loop >= maxLoops)
				{
					targetSpeed = 0;
				}
			}
					
			// Werte an AudioManager übergeben (thread-sicher)
			if (AudioManager != null)
			{
				AudioManager.FillBuffer((float)delta, currentSpeed / maxLoops, loop / maxLoops);
			}
			Thread.Sleep(2); // ca. 500 Hz
		}
		sw.Stop();
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
				GD.Print(_leftMoved, motorRunning);
				if (!_leftMoved)
				{
					if (motorRunning)
						targetSpeed = 0f;
					else
						targetSpeed = motorSpeed / 60.0f;
					motorRunning = !motorRunning;
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
		if (Mathf.Abs(currentSpeed) < 0.001f)
		{
			if (targetSpeed == 0)
			{
				StopMotor();
			}
			else if (Math.Abs(targetSpeed) > 0)
			{
				StartMotor();
			}
		}
		else
		{
			QueueRedraw();
		}

		_lastLoop = loop;

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
	}

	private Font _defaultFont = ThemeDB.FallbackFont;
	public override void _Draw()
	{
		record.Rotation = loop % 1 * Mathf.Pi * 2;
		needle.Position = new Vector2((1 - (loop / maxLoops)) * 125 + 60, -12);
		// Text für Sample-Länge und aktuellen Index zeichnen
		string info = $"Max Loop: {maxLoops} | Loop: {loop} | Speed: {currentSpeed} | Target Speed: {targetSpeed}";
		DrawString(_defaultFont, new Vector2(-240, 240), info, HorizontalAlignment.Center);
	}
}
