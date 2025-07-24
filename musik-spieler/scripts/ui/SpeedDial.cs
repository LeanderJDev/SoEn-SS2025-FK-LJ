using Godot;
using System;
using Musikspieler.Scripts.RecordView;

namespace Musikspieler.Scripts.UI
{
	public partial class SpeedDial : StaticBody3D
	{
		[Export]
		public float SpeedScale = 1.0f;
		[Export]
		public float AngleScale = 0.01f;

		[Signal]
		public delegate void SpeedChangeEventHandler(float volume);
		[Signal]
		public delegate void SpeedResetEventHandler();

		private bool isBeingMoved = false;
		private float lastAngle;
		private float dialAngle;

		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left)
				{
					if (mouseEvent.DoubleClick && RaycastHandler.IsObjectUnderCursor(this))
					{
						EmitSignal(SignalName.SpeedReset);
						return;
					}
					if (mouseEvent.Pressed && RaycastHandler.IsObjectUnderCursor(this))
					{
						lastAngle = mouseEvent.Position.X * Mathf.Pi * AngleScale;
						isBeingMoved = true;
					}
					else if (isBeingMoved)
					{
						isBeingMoved = false;
					}
				}
			}
			if (@event is InputEventMouseMotion mouseMotion)
			{
				if (isBeingMoved)
				{
					float angle = mouseMotion.Position.X * Mathf.Pi * AngleScale;
					float deltaAngle = angle - lastAngle;
					lastAngle = angle;

					RotateZ(deltaAngle);

					EmitSignal(SignalName.SpeedChange, deltaAngle / 2.0f * Mathf.Pi * SpeedScale);
				}
			}
		}
	}
}
