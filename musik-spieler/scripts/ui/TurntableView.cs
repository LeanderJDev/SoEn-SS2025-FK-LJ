using Godot;
using System;
using Musikspieler.Scripts.Audio;
using Musikspieler.Scripts.RecordView;

namespace Musikspieler.Scripts.UI
{
	public partial class TurntableView : Node3D
	{
		[Export]
		public TurntableAudioManager turntableAudioManager { get; set; }
		[Export]
		public Node3D Tonearm { get; set; }
		[Export]
		public Node3D Record { get; set; }
		// Die Rotationen werden im Editor kalibriert
		[Export] public float OuterLimitYAngle { get; set; }
		[Export] public float RecordStartYAngle { get; set; }
		[Export] public float InnerLimitYAngle { get; set; }
		[Export] public float LiftedXAngle { get; set; }
		[Export] public float LoweredXAngle { get; set; }
		[Export] public AudioStreamWav testAudio { get; set; }

		private float restAngleY;
		private float outerLimitYAngle;
		private float recordStartYAngle;
		private float innerLimitYAngle;
		private float liftXAngle;

		private TurntableController controller;

		private bool isLeftMouseDown = false;
		private bool isRightMouseDown = false;

		private float tonearmYAngleOffset;
		private float lastRightDragAngle;

		public override void _Ready()
		{
			if (turntableAudioManager == null)
			{
				GD.PrintErr("TurntableAudioManager not set!");
				return;
			}

#if DEBUG
			Song song = new Song("Song", "Album", "Artist", 0, "", audioStream: testAudio);
			turntableAudioManager.SetSong(song);
#endif

			controller = new TurntableController(turntableAudioManager.Turntable, turntableAudioManager.AudioPlayer);

			restAngleY = Tonearm.Rotation.Y;
			outerLimitYAngle = Mathf.Wrap(Mathf.DegToRad(OuterLimitYAngle), -Mathf.Pi, Mathf.Pi);
			recordStartYAngle = Mathf.Wrap(Mathf.DegToRad(RecordStartYAngle), -Mathf.Pi, Mathf.Pi);
			innerLimitYAngle = Mathf.Wrap(Mathf.DegToRad(InnerLimitYAngle), -Mathf.Pi, Mathf.Pi);
			float liftedXAngle = Mathf.Wrap(Mathf.DegToRad(LiftedXAngle) - Tonearm.Rotation.X, -Mathf.Pi, Mathf.Pi);
			float loweredXAngle = Mathf.Wrap(Mathf.DegToRad(LoweredXAngle) - Tonearm.Rotation.X, -Mathf.Pi, Mathf.Pi);
			liftXAngle = loweredXAngle - liftedXAngle;
		}

		public override void _Process(double delta)
		{
			Record.RotateY(-1 * turntableAudioManager.Turntable.CurrentLoop % 1 * Mathf.Pi * 2 - Record.Rotation.Y);
			// Don't apply if Tonearm is grabbed or resting
			if (
				!controller.IsArmGrabbed &&
				Tonearm.Rotation.Y < recordStartYAngle &&
				Tonearm.Rotation.Y > innerLimitYAngle
			)
			{
				// trust me bro this works
				Tonearm.RotateY((1 - turntableAudioManager.Turntable.CurrentSongPosition) * (recordStartYAngle - innerLimitYAngle) - (outerLimitYAngle - recordStartYAngle) - Tonearm.Rotation.Y);
			}
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventMouseButton btn)
			{
				if (btn.ButtonIndex == MouseButton.Left)
				{
					if (btn.Pressed && !isLeftMouseDown)
					{
						isLeftMouseDown = true;
						OnLeftMouseDown(btn.Position);
					}
					else
					{
						isLeftMouseDown = false;
						OnLeftMouseUp(btn.Position);
					}
				}
				if (btn.ButtonIndex == MouseButton.Right)
				{
					if (btn.Pressed && !isRightMouseDown)
					{
						isRightMouseDown = true;
						OnRightMouseDown(btn.Position);
					}
					else
					{
						isRightMouseDown = false;
						OnRightMouseUp();
					}
				}
			}
			if (@event is InputEventMouseMotion motion)
			{
				if (isLeftMouseDown) OnLeftMouseDrag(motion.Position);
				if (isRightMouseDown) OnRightMouseDrag(motion.Position);
			}
		}

		public void OnLeftMouseDown(Vector2 mousePos)
		{
			if (Utility.CameraRaycast(GetViewport().GetCamera3D(), new Mask<CollisionMask>(CollisionMask.ToneArm), out var result))
			{
#if DEBUG
				GD.Print("ToneArm hit", result["position"]);
#endif
				Tonearm.RotateObjectLocal(new Vector3(1, 0, 0), liftXAngle);

				tonearmYAngleOffset = RaycastHandler.MouseToTargetAngle(Tonearm, mousePos) - Tonearm.Rotation.Y;
				controller.DragArmBegin();
			}
		}

		public float TonarmMousePos(Vector2 mousePos)
		{
			float angle = RaycastHandler.MouseToTargetAngle(Tonearm, mousePos);
			angle = angle - tonearmYAngleOffset;
			angle = Mathf.Wrap(angle + Mathf.Pi, -Mathf.Pi, Mathf.Pi);

			angle = Mathf.Clamp(angle, innerLimitYAngle, outerLimitYAngle);

			// Map angle between recordStartYAngle and innerLimitYAngle to 0..1
			float pos = Mathf.InverseLerp(recordStartYAngle, innerLimitYAngle, angle);
			return pos;
		}
		public void OnLeftMouseUp(Vector2 mousePos)
		{
			if (controller.IsArmGrabbed)
			{
				float pos = TonarmMousePos(mousePos);
				pos = controller.DragArmEnd(pos);
				float angle = Mathf.Lerp(recordStartYAngle, innerLimitYAngle, pos);
				if (pos == -1)
				{
					angle = restAngleY;
				}
				Tonearm.RotateY(angle - Tonearm.Rotation.Y + Mathf.Pi);
				Tonearm.RotateObjectLocal(new Vector3(1, 0, 0), -liftXAngle);
			}
		}

		public void OnLeftMouseDrag(Vector2 mousePos)
		{
			if (controller.IsArmGrabbed)
			{
				// Map angle between recordStartYAngle and innerLimitYAngle to 0..1
				float pos = TonarmMousePos(mousePos);
				float angle = Mathf.Lerp(recordStartYAngle, innerLimitYAngle, pos);
				controller.DragArm(pos);
				Tonearm.RotateY(angle - Tonearm.Rotation.Y + Mathf.Pi);
			}
		}

		public void OnRightMouseDown(Vector2 mousePos)
		{
			if (Utility.CameraRaycast(GetViewport().GetCamera3D(), new Mask<CollisionMask>(CollisionMask.RecordPlatter), out var result))
			{
#if DEBUG
				GD.Print("Platter hit", result["position"]);
#endif
				controller.DragPlatterBegin();
				lastRightDragAngle = RaycastHandler.MouseToTargetAngle(Record, mousePos);
			}
		}
		public void OnRightMouseUp()
		{
			if (controller.IsPlatterGrabbed)
			{
				controller.DragPlatterEnd();
			}
		}
		public void OnRightMouseDrag(Vector2 mousePos)
		{
			if (controller.IsPlatterGrabbed)
			{
				float angle = RaycastHandler.MouseToTargetAngle(Record, mousePos);
				float angleDelta = -1 * Mathf.Wrap(angle - lastRightDragAngle, -Mathf.Pi, Mathf.Pi) / (2 * Mathf.Pi);
				lastRightDragAngle = angle;

				controller.DragPlatter(angleDelta);
			}
		}

		public void OnMotorOff()
		{
			controller.StopMotor();
		}

		public void OnMotorOn()
		{
			controller.StartMotor();
		}

		public void OnVolumeChange(float volume)
		{
			controller.SetVolume(volume);
		}

		public void OnSpeedChange(float speed)
		{
			controller.ChangeSpeed(speed);
		}

		public void OnSpeedReset()
		{
			controller.ResetSpeed();
		}
	}
}
