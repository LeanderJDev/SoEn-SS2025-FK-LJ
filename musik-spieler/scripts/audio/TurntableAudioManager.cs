using System;
using System.Threading;
using Godot;

namespace Musikspieler.Scripts.Audio
{
    public partial class TurntableAudioManager : Node2D
    {
        [Export]
        public MeshInstance3D RecordOnPlayer;
        [Export]
        public StandardMaterial3D coverImageMaterial;
        private AudioPlayer audioPlayer;
        private Turntable turntable;
        private ISong currentSong;
        private Thread thread;
        private volatile bool threadRunning;
        public ITurntable Turntable => turntable;
        public IAudioPlayer AudioPlayer => audioPlayer;

        public void SetSong(ISong song)
        {
            if (currentSong != song) {
                if(currentSong != null) currentSong.DisposeAudio();
                currentSong = song;
                // Dieser null Check hilft bei Debugging Hotloads und ist ansonsten nicht nötig
                if (song.Audio == null) currentSong.LoadAudio();
                audioPlayer.SetSample(currentSong.Audio);
                turntable.SetMaxLoops(audioPlayer.SampleLength / audioPlayer.SampleRate);
                if (song.CoverData != null) {
                    Image image = new Image();
                    image.LoadJpgFromBuffer(song.CoverData);
                    ImageTexture texture = ImageTexture.CreateFromImage(image);
                    coverImageMaterial.AlbedoTexture = texture;
                    RecordOnPlayer.SetSurfaceOverrideMaterial(0, coverImageMaterial);
                }
            }
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
                        (int)(delta * audioPlayer.SampleRate / 1000 * (1 + (float)delta)) + 1; // + 1 to adjust for int clamping

                    while (audioPlayer.FramesAvailable > 0 && samplesToWrite > 0)
                    {
                        turntable.SimulationStep(1.0f / audioPlayer.SampleRate);
                        audioPlayer.PlaySample(
                            (int)(turntable.CurrentSongPosition * audioPlayer.SampleLength)
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
