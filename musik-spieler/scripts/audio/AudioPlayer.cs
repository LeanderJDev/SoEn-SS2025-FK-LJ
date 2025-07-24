using System;
using Godot;

namespace Musikspieler.Scripts.Audio
{
    public interface IAudioPlayer
    {
        int SampleLength { get; }
        int SampleRate { get; }
        int FramesAvailable { get; }
        float Volume { get; set; }
        bool Mute { get; set; }
    }

    public partial class AudioPlayer : AudioStreamPlayer, IAudioPlayer
    {
        private AudioStream sample;
        private AudioStreamGenerator generator;
        private AudioStreamGeneratorPlayback playback;

        private int sampleRate = 44100;

        private Vector2[] samples;
        private float volume;

        public int SampleLength => samples != null ? samples.Length : -1;
        public int SampleRate => sampleRate;
        public int FramesAvailable => playback.GetFramesAvailable();

        public float Volume
        {
            get { return VolumeLinear; }
            set { volume = VolumeLinear = value; }
        }

        public bool Mute
        {
            get { return VolumeLinear == 0.0f; }
            set { VolumeLinear = value ? 0.0f : volume; }
        }

        public void SetSample(AudioStreamWav sample)
        {
            if (sample == null || sample.Format != AudioStreamWav.FormatEnum.Format16Bits)
            {
                GD.PrintErr("AudioPlayer: Etwas ist schief gelaufen. Sample ist null oder keine PCM16-WAV-Datei.", sample);
                return;
            }
            if (sample.MixRate != sampleRate)
            {
                GD.PrintErr($"AudioPlayer: Etwas ist schief gelaufen. sample.MixRate ist nicht {sampleRate}Hz.");
            }
            var data = sample.Data;
            var channelCount = sample.Stereo ? 2 : 1;
            var sampleCount = data.Length / 2 / channelCount;

#if DEBUG
            GD.Print($"sample.Data.Length (Bytes): {data.Length}");
            GD.Print($"sample.MixRate: {sample.MixRate}");
            GD.Print($"sample.Format: {sample.Format}");
            GD.Print($"AudioStreamWav.GetLength: {sample.GetLength()}");
            GD.Print($"sample.Stereo: {sample.Stereo}");
            GD.Print($"Berechnete Sample-Anzahl: {sampleCount}");
#endif

            Vector2[] samplesData = new Vector2[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float left = BitConverter.ToInt16(data, i * 2 * channelCount) / 32768.0f;
                float right =
                    channelCount == 2
                        ? BitConverter.ToInt16(data, i * 2 * channelCount + 2) / 32768.0f
                        : left;
                samplesData[i] = new Vector2(left, right);
            }

            samples = samplesData;
#if DEBUG
            GD.Print($"samples.Length: {samples.Length}");
#endif
        }

        public void PlaySample(int index)
        {
            Vector2 sample = samples[Math.Clamp((int)index, 0, samples.Length - 1)];
            playback.PushFrame(sample);
        }

        public override void _Ready()
        {
            generator = new AudioStreamGenerator();
            generator.MixRate = sampleRate;
            generator.BufferLength = 0.1f; // Nur kurzer Buffer da Playback Live verändert
            Stream = generator;
            Volume = 1.0f;
            Mute = true;
            Play();

            playback = (AudioStreamGeneratorPlayback)GetStreamPlayback();
        }
    }
}
