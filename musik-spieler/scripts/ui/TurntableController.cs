using Godot;
using System;
using Musikspieler.Scripts.Audio;

namespace Musikspieler.Scripts.UI
{
	public class TurntableController
	{
		private ITurntable turntable;
		private IAudioPlayer audioPlayer;

		private bool armGrabbed;
		public bool IsArmGrabbed => armGrabbed;
		private bool platterGrabbed;
		public bool IsPlatterGrabbed => platterGrabbed;

		private bool motorStateBeforeDragPlatter;
		private const float motorStartThreshold = -0.2f;
		private bool tonearmAboveRecord = false;

		public TurntableController(ITurntable turntable, IAudioPlayer audioPlayer)
		{
			this.turntable = turntable;
			this.audioPlayer = audioPlayer;
		}

		public void DragArmBegin()
		{
			armGrabbed = true;
			audioPlayer.Mute = true;
		}

		public void DragArm(float armPos)
		{
			if (!turntable.IsMotorRunning && armPos > motorStartThreshold)
			{
				turntable.SetMotorState(true);
			}
			else if (turntable.IsMotorRunning && armPos < motorStartThreshold)
			{
				turntable.SetMotorState(false);
			}
			tonearmAboveRecord = 0.0f <= armPos && armPos <= 1.0f;
			if (tonearmAboveRecord)
			{
				turntable.MoveArm(armPos);
			}
		}

		public float DragArmEnd(float armPos)
		{
			armGrabbed = false;

			// Snapping zur Tonarmablage / zur Schallplatte
			armPos = armPos > motorStartThreshold ? Mathf.Clamp(armPos, 0.0f, 1.0f) : -1;

			// Übernehme Snapping für Turntable
			DragArm(armPos);

			audioPlayer.Mute = !tonearmAboveRecord;
			return armPos;
		}

		public void DragPlatterBegin()
		{
			platterGrabbed = true;
			motorStateBeforeDragPlatter = turntable.IsMotorRunning;
			turntable.ScratchTarget(0);
		}

		public void DragPlatter(float angleDelta)
		{
			turntable.ScratchTarget(angleDelta);
		}

		public void DragPlatterEnd()
		{
			platterGrabbed = false;
			if (motorStateBeforeDragPlatter)
			{
				turntable.SetMotorState(true);
				turntable.BoostSpeed(0.3f);
			}
			turntable.EndScratch();
		}

		public void StopMotor()
		{
			turntable.SetMotorState(false);
		}

		public void StartMotor()
		{
			turntable.SetMotorState(true);
		}

		public void SpeedDial(float angle)
		{
			turntable.ChangeMotorSpeed(10 * angle / (Mathf.Pi * 2));
		}

		public void VolumeDial(float angle)
		{
			audioPlayer.Volume = 10 * angle / (Mathf.Pi * 2);
		}
	}
}
