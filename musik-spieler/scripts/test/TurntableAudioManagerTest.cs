using System;
using Godot;
using Musikspieler.Scripts.Audio;

namespace Musikspieler.Scripts.Test
{
    public partial class TurntableAudioManagerTest : Node2D
    {
        [Export]
        public AudioStreamWav sample;

        [Export]
        public TurntableAudioManager turntableAudioManager;

        public override async void _Ready()
        {
            GD.Print("Test readying");
            if (turntableAudioManager == null)
            {
                GD.PrintErr("TurntableAudioManager not found as child!");
                return;
            }
            Song song = new Song("Song", "Album", "Artist", "", sample);
            turntableAudioManager.SetSong(song);
            while (turntableAudioManager.Turntable == null)
            {
                GD.Print("Waiting");
                await ToSignal(GetTree().CreateTimer(1.0), "timeout");
            }
            turntableAudioManager.Turntable.SetMotorState(true);
#if DEBUG
            GD.Print("Test ready");
            GD.Print(turntableAudioManager.Turntable.MaxLoops);
            GD.Print(turntableAudioManager.Turntable.IsMotorRunning);
            GD.Print(turntableAudioManager.Turntable.CurrentLoop);
#endif
        }

        public override void _Process(double delta)
        {
            QueueRedraw();
        }

        private Font _defaultFont = ThemeDB.FallbackFont;

        public override void _Draw()
        {
            string info = $"CurrentLoop: {turntableAudioManager.Turntable.CurrentLoop}";
            DrawString(_defaultFont, new Vector2(100, 30), info, HorizontalAlignment.Center);
        }
    }
}
