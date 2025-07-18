using System;
using System.Threading;
using Godot;

namespace Musikspieler.Scripts.Audio
{
    public partial class TurntableAudioManager : Node2D
    {
        private AudioPlayer audioPlayer;
        private Turntable turntable;
        private Song currentSong;
        private Thread thread;
        private bool threadRunning;
        public ITurntable Turntable => turntable;

        public void SetSong(Song song)
        {
            currentSong = song;
            audioPlayer.SetSample(song.Audio);
            turntable.SetMaxLoops(audioPlayer.SampleLength / audioPlayer.SampleRate);
        }

        private void ThreadLoop()
        {
#if DEBUG
            GD.Print("Thread started");
#endif
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            double lastTime = sw.Elapsed.TotalMilliseconds;
            // Can be adjusted for higher update frequency or better performance for the rest of the process
            int delay = 1;
            while (threadRunning)
            {
                double delta = sw.Elapsed.TotalMilliseconds - lastTime;
                if (audioPlayer.SampleLength != -1)
                {
                    int samplesToWrite =
                        (int)(delay * audioPlayer.SampleRate / 1000 * (1 + (float)delta)) + 1; // + 1 to adjust for int clamping

                    while (samplesToWrite > 0)
                    {
                        turntable.SimulationStep(1.0f / audioPlayer.SampleRate);
                        audioPlayer.PlaySample(
                            (int)(turntable.GetCurrentSongPosition() * audioPlayer.SampleLength)
                        );
                        samplesToWrite--;
                    }
                }
                lastTime = sw.Elapsed.TotalMilliseconds;
                Thread.Sleep(delay);
            }
#if DEBUG
            GD.Print("Thread stopped");
#endif
        }

        public override void _Ready()
        {
            audioPlayer = new AudioPlayer();
            AddChild(audioPlayer);
            turntable = new Turntable();

            threadRunning = true;
            thread = new Thread(ThreadLoop);
            thread.Start();
        }

        public override void _ExitTree()
        {
            threadRunning = false;
            thread?.Join();
        }
    }
}
