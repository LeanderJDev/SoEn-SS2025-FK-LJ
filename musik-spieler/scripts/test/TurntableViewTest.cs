using Godot;
using System;
using Musikspieler.Scripts.development;
using Musikspieler.Scripts.UI;
using Musikspieler.Scripts.Audio;
using System.Reflection;

namespace Musikspieler.Scripts.Test
{
	public partial class TurntableViewTest : Node3D
	{
		[Export]
		public TurntableView turntableView { get; set; }

		private Plot samplePlot;
		private Plot speedPlot;

		public override void _Ready()
		{
			samplePlot = new Plot("Samples", 50, 100, scaleY: 400f, length: 1000);
			AddChild(samplePlot);
			speedPlot = new Plot("TurntableSpeed", 50, 300, scaleY: 50f, length: 500, color: new Color(0, 0, 1, 0.4f));
			AddChild(speedPlot);

			turntableView.turntableAudioManager.SetSong(MusicCollection.Instance.PlaylistDirectory["Whole Collection"][2]);
		}

		public override void _PhysicsProcess(double delta)
		{
			// Das hier ist ein wenig brutal, aber ich wollte möglichst viel Debug Code aus den Klassen rauslassen
			// Hole das private Feld "audioPlayer" aus turntableAudioManager
			var audioPlayerField = turntableView.turntableAudioManager.GetType().GetField("audioPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
			var audioPlayer = audioPlayerField.GetValue(turntableView.turntableAudioManager);

			// Hole das Feld "samples" aus audioPlayer
			var samplesField = audioPlayer.GetType().GetField("samples", BindingFlags.NonPublic | BindingFlags.Instance);
			var samples = (Vector2[])samplesField.GetValue(audioPlayer);

			int sampleIndex = (int)(turntableView.turntableAudioManager.Turntable.CurrentSongPosition * samples.Length);

			// Bereich berechnen, der im letzten Frame gespielt wurde
			float loopsPlayed = turntableView.turntableAudioManager.Turntable.CurrentSpeed * (float)delta;
			int samplesPlayed = (int)(Math.Abs(loopsPlayed) * samples.Length / (float)turntableView.turntableAudioManager.Turntable.MaxLoops);
			int direction = loopsPlayed >= 0 ? 1 : -1;

			for (int i = 0; i < samplesPlayed; i++)
			{
				int idx = (sampleIndex + direction * i + samples.Length) % samples.Length;
				Vector2 s = samples[idx];
				float normalized = (s.X + s.Y) / 2f;
				samplePlot.AddValue(normalized);
			}

			speedPlot.AddValue(turntableView.turntableAudioManager.Turntable.CurrentSpeed);
		}
	}
}