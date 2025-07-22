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

		public TurntableController(ITurntable turntable, IAudioPlayer audioPlayer)
		{
			this.turntable = turntable;
			this.audioPlayer = audioPlayer;
		}

		public void DragArmBegin()
		{
			armGrabbed = true;
			audioPlayer.Paused = true;
		}

		public void DragArm(float armPos)
		{
			turntable.MoveArm(armPos);
		}

		public void DragArmEnd()
		{
			armGrabbed = false;
			audioPlayer.Paused = false;
		}

		public void DragPlatterBegin()
		{
			platterGrabbed = true;
			motorStateBeforeDragPlatter = turntable.IsMotorRunning;
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

		public void StopButton()
		{
			turntable.SetMotorState(false);
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
