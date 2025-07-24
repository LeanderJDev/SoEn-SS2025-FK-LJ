using Godot;
using System;
using Musikspieler.Scripts.RecordView;
using Musikspieler.Scripts.Audio;

namespace Musikspieler.Scripts.UI
{
	public partial class StartStopButton : StaticBody3D
	{
		[Export]
		public float downHeight = -0.02f;
		[Export]
		public float upHeight = 0;
		[Export]
		public TurntableAudioManager turntableAudioManager;

		[Signal]
		public delegate void MotorOffEventHandler();

		[Signal]
		public delegate void MotorOnEventHandler();

		private Tween animationTween;

		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent && RaycastHandler.IsObjectUnderCursor(this))
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left)
				{
					if (mouseEvent.Pressed)
					{
						// Animate down
						AnimateDown();
						if (!turntableAudioManager.Turntable.IsMotorRunning)
						{
							EmitSignal(SignalName.MotorOn);
						}
						else
						{
							EmitSignal(SignalName.MotorOff);
						}
					}
					else
					{
						// Animate up and toggle state
						AnimateUp();
					}
				}
			}
		}

		private void AnimateDown()
		{
			if (animationTween != null) animationTween.Kill();
			animationTween = CreateTween();
			animationTween.TweenProperty(this, "position:y", downHeight, 0.1);
		}

		private void AnimateUp()
		{
			if (animationTween != null) animationTween.Kill();
			animationTween = CreateTween();
			animationTween.TweenProperty(this, "position:y", upHeight, 0.1);
		}
	}
}