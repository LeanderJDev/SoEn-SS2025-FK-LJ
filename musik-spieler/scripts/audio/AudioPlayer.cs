using System;
using Godot;

namespace Musikspieler.Scripts.Audio
{
    public partial class AudioPlayer : AudioStreamPlayer
    {
        private AudioStream sample;
        private AudioStreamGenerator generator;
        private AudioStreamGeneratorPlayback playback;

        private int sampleIndex;
        private int sampleRate = 44100;

        private Vector2[] samples;

        public int SampleLength => samples != null ? samples.Length : -1;
        public int SampleRate => sampleRate;

        public void SetSample(AudioStreamWav sample)
        {
            if (sample == null || sample.Format != AudioStreamWav.FormatEnum.Format16Bits)
            {
                GD.PrintErr("Etwas ist schief gelaufen. Sample ist keine PCM16-WAV-Datei.");
                return;
            }
            var data = sample.Data;
            var channelCount = sample.Stereo ? 2 : 1;
            var sampleCount = data.Length / 2 / channelCount;

#if DEBUG
            GD.Print($"wav.Data.Length (Bytes): {data.Length}");
            GD.Print($"wav.MixRate: {sample.MixRate}");
            GD.Print($"wav.Format: {sample.Format}");
            GD.Print($"AudioStreamWav.GetLength: {sample.GetLength()}");
            GD.Print($"wav.Stereo: {sample.Stereo}");
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
            sampleIndex = index;
            Vector2 sample = samples[Math.Clamp((int)sampleIndex, 0, samples.Length - 1)];
            playback.PushFrame(sample);
        }

        public override void _Ready()
        {
            generator = new AudioStreamGenerator();
            generator.MixRate = sampleRate;
            generator.BufferLength = 0.1f; // Nur kurzer Buffer da Playback Live verändert
            Stream = generator;
            VolumeLinear = 0.3f;
            Play();

            playback = (AudioStreamGeneratorPlayback)GetStreamPlayback();
        }
    }
}
