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
			Song song = new Song("Song", "Album", "Artist", "", audioStream: testAudio);
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
			Record.RotateY(-1*turntableAudioManager.Turntable.CurrentLoop % 1 * Mathf.Pi * 2 - Record.Rotation.Y);
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

				tonearmYAngleOffset = MouseAngleCameraRaycast(Tonearm, mousePos) - Tonearm.Rotation.Y;
				controller.DragArmBegin();
			}
		}
		public void OnLeftMouseUp(Vector2 mousePos)
		{
			if (controller.IsArmGrabbed) {
				float angle = MouseAngleCameraRaycast(Tonearm, mousePos);
				angle = angle - tonearmYAngleOffset;
				angle = Mathf.Wrap(angle + Mathf.Pi, -Mathf.Pi, Mathf.Pi);

				angle = Mathf.Clamp(angle, innerLimitYAngle, outerLimitYAngle);

				// Map angle between recordStartYAngle and innerLimitYAngle to 0..1
				float pos = Mathf.InverseLerp(recordStartYAngle, innerLimitYAngle, angle);
				pos = controller.DragArmEnd(pos);
				angle = Mathf.Lerp(recordStartYAngle, innerLimitYAngle, pos);
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
				float angle = MouseAngleCameraRaycast(Tonearm, mousePos);
				angle = angle - tonearmYAngleOffset;
				angle = Mathf.Wrap(angle + Mathf.Pi, -Mathf.Pi, Mathf.Pi);

				angle = Mathf.Clamp(angle, innerLimitYAngle, outerLimitYAngle);

				// Map angle between recordStartYAngle and innerLimitYAngle to 0..1
				float pos = Mathf.InverseLerp(recordStartYAngle, innerLimitYAngle, angle);
				controller.DragArm(pos);
				// TODO Verstehen was hier los ist und Magische Variablen entfernen
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
				lastRightDragAngle = MouseAngleCameraRaycast(Record, mousePos);
			}
		}
		public void OnRightMouseUp()
		{
			if (controller.IsPlatterGrabbed) {
				controller.DragPlatterEnd();
			}
		}
		public void OnRightMouseDrag(Vector2 mousePos)
		{
			if (controller.IsPlatterGrabbed)
			{
				float angle = MouseAngleCameraRaycast(Record, mousePos);
				float angleDelta = -1 * Mathf.Wrap(angle - lastRightDragAngle, -Mathf.Pi, Mathf.Pi) / (2 * Mathf.Pi);
				lastRightDragAngle = angle;

				controller.DragPlatter(angleDelta);
			}
		}

		private void OnStopButton()
		{
			controller.StopButton();
		}

		private float MouseAngleCameraRaycast(Node3D target, Vector2 mousePos)
		{
			// 1. Ray von Kamera durch Maus
			float targetY = target.GlobalPosition.Y;
			Camera3D cam = GetViewport().GetCamera3D();
			Vector3 rayOrigin = cam.ProjectRayOrigin(mousePos);
			Vector3 rayDir = cam.ProjectRayNormal(mousePos);

			// 2. Schnittpunkt mit Platten-Ebene (z.B. y = Plattenhöhe)
			float t = (targetY - rayOrigin.Y) / rayDir.Y;
			Vector3 hit = rayOrigin + rayDir * t;

			// 3. Berechne Winkel zum Zentrum des target
			Vector3 center = target.GlobalTransform.Origin;
			Vector3 dir = (hit - center).Normalized();
			float angle = Mathf.Atan2(dir.X, dir.Z); // Winkel zur Z-Achse

			return angle;
		}
	}
}
