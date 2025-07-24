using Godot;
using System;
using Musikspieler.Scripts.RecordView;

namespace Musikspieler.Scripts.UI
{
	public partial class VolumeKnob : StaticBody3D
	{
		[Export]
		public float Angle0 = 135.0f;
		[Export]
		public float Angle1 = -135.0f;
		[Export]
		public float VolumeScale = 2.0f;
		[Export]
		public float Volume = 0.3f;

		[Signal]
		public delegate void VolumeChangeEventHandler(float volume);

		private bool isBeingMoved = false;
		private float lastAngle;

		private float knobAngle;

		public override void _Ready()
		{
			GD.Print(Volume);
			knobAngle = Mathf.DegToRad(Mathf.Lerp(Angle0, Angle1, Volume));
			RotateY(knobAngle - Rotation.Y);
			// This would need to wait for the TurntableView to finish it's _Ready method
			// For now it just doesn't set Volume at Startup
			// EmitSignal(SignalName.VolumeChange, Volume);
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left)
				{
					if (mouseEvent.Pressed && RaycastHandler.IsObjectUnderCursor(this))
					{
						lastAngle = RaycastHandler.MouseToTargetAngle(this, mouseEvent.Position);
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
					float mouseAngle = RaycastHandler.MouseToTargetAngle(this, mouseMotion.Position);
					float delta = Mathf.Wrap(mouseAngle - lastAngle, -Mathf.Pi, Mathf.Pi);
					lastAngle = mouseAngle;

					float minAngle = Mathf.DegToRad(Mathf.Min(Angle0, Angle1));
					float maxAngle = Mathf.DegToRad(Mathf.Max(Angle0, Angle1));

					// Nur bewegen, wenn nicht am Limit oder Bewegung in Richtung Bereich
					if (knobAngle <= minAngle && delta < 0)
					{
						knobAngle = minAngle;
					}
					else if (knobAngle >= maxAngle && delta > 0)
					{
						knobAngle = maxAngle;
					}
					else
					{
						knobAngle = Mathf.Clamp(knobAngle + delta, minAngle, maxAngle);
					}
					RotateY(knobAngle - Rotation.Y);

					Volume = Mathf.InverseLerp(Angle0, Angle1, Mathf.RadToDeg(Rotation.Y));
					EmitSignal(SignalName.VolumeChange, Volume * VolumeScale);
				}
			}
		}
	}
}
