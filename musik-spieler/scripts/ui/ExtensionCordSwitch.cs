using Godot;
using System;
using Musikspieler.Scripts.RecordView;
using Musikspieler.Scripts.Audio;

namespace Musikspieler.Scripts.UI{
	public partial class ExtensionCordSwitch : StaticBody3D
	{
		[Export]
		public Light3D light;
		[Export]
		public float onAngle = -25.0f;
		[Export]
		public float offAngle = 5.0f;

		[Signal]
		public delegate void MotorOffEventHandler();


		private bool isOff = false;
		private Tween animationTween;
		private float lightEnergy;

		public override void _Ready()
		{
			lightEnergy = light.LightEnergy;
		}
		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (RaycastHandler.IsObjectUnderCursor(this))
				{
					GD.Print(isOff);
					if (isOff)
					{
						if (animationTween != null) animationTween.Kill();
						On();
						isOff = false;
					}
					else
					{
						if (animationTween != null) animationTween.Kill();
						Off();
						isOff = true;
						EmitSignal(SignalName.MotorOff);
					}

				}
			}
		}

		private void Off()
		{
			animationTween = CreateTween();
			animationTween.TweenProperty(this, "rotation_degrees:x", RotationDegrees.X + (offAngle - onAngle), 0.1);

			// Dim the light over 0.5 seconds
			if (light != null)
			{
				animationTween.TweenProperty(light, "light_energy", 0, 0.5).SetDelay(0.2);
			}

			// Quit the game after the animation finishes
			animationTween.TweenCallback(Callable.From(() => GetTree().Quit())).SetDelay(0.7);
		}

		private void On()
		{
			animationTween = CreateTween();
			animationTween.TweenProperty(this, "rotation_degrees:x", RotationDegrees.X + (onAngle - offAngle), 0.1);

			// Dim the light over 0.5 seconds
			if (light != null)
			{
				animationTween.TweenProperty(light, "light_energy", lightEnergy, 0.5).SetDelay(0.2);
			}
		}
	}
}