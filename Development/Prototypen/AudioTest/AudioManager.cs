using Godot;
using System;
using System.Threading;
using Simulation;

public partial class AudioManager : Node2D
{
    [Export] public AudioStream Sample; // Typ geändert!
    private AudioStreamGenerator _generator;
    private AudioStreamGeneratorPlayback _playback;
    private AudioStreamPlayer _player;
    private Vector2[] _samples; // Stereo: X = links, Y = rechts
    private const int sampleRate = 44100;
    private volatile float _sampleIndex = 0;
    private volatile float _speed = 0;

    private const int WaveformLength = 200;
    private float[] _waveformBuffer = new float[WaveformLength];
    private int _waveformIndex = 0;

    private const int indexDifferenceLength = 200;
    private int[] _indexDifferencePlot = new int[indexDifferenceLength];
    private int _indexDifferenceIndex = 0;

    private const int deltaLength = 200;
    private float[] _deltaPlot = new float[deltaLength];
    private int _deltaIndex = 0;


    public int SampleLength => _samples.Length;

    private Thread _thread;
    private volatile bool _threadRunning = false;

    public Turntable turntable;

    private float indexDelta = 0;

    public override void _Ready()
    {
        // AudioStreamGenerator initialisieren
        _generator = new AudioStreamGenerator();
        _generator.MixRate = 44100;
        _generator.BufferLength = 0.1f;
        _player = new AudioStreamPlayer();
        AddChild(_player);
        _player.Stream = _generator;
        _player.VolumeLinear = 0.5f;
        _player.Play();

        _playback = (AudioStreamGeneratorPlayback)_player.GetStreamPlayback();

        // Samples aus AudioStreamWav extrahieren
        var wav = Sample as AudioStreamWav;
        if (wav == null || wav.Format != AudioStreamWav.FormatEnum.Format16Bits)
        {
            GD.PrintErr("Sample ist keine PCM16-WAV-Datei! Bitte Import-Einstellungen prüfen.");
            return;
        }
        var data = wav.Data;
        GD.Print($"wav.Data.Length (Bytes): {data.Length}");
        GD.Print($"wav.MixRate: {wav.MixRate}");
        GD.Print($"wav.Format: {wav.Format}");
        GD.Print($"AudioStreamWav.GetLength: {Sample.GetLength()}");
        GD.Print($"wav.Stereo: {wav.Stereo}");
        var channelCount = wav.Stereo ? 2 : 1;
        var sampleCount = data.Length / 2 / channelCount;
        GD.Print($"Berechnete Sample-Anzahl: {sampleCount}");

        _samples = new Vector2[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float left = BitConverter.ToInt16(data, i * 2 * channelCount) / 32768.0f;
            float right = channelCount == 2 ? BitConverter.ToInt16(data, i * 2 * channelCount + 2) / 32768.0f : left;
            _samples[i] = new Vector2(left, right);
            if (i < 10)
                GD.Print($"Sample[{i}]: L={left}, R={right}");
        }
        GD.Print($"_samples.Length: {_samples.Length}");

        turntable = new Turntable(SampleLength / 44100);

        _threadRunning = true;
        _thread = new Thread(ThreadLoop);
        _thread.Start();
    }

    public override void _ExitTree()
    {
        _threadRunning = false;
        _thread?.Join();
    }

    private void ThreadLoop()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        double lastTime = sw.Elapsed.TotalSeconds;
        int delay = 0;
        while (_threadRunning)
        {
            int samplesToWrite = ((delay * sampleRate)/1000)+1;
            while (_playback.GetFramesAvailable() > 0 && samplesToWrite > 0)
            {
                float prevIndex = _sampleIndex;
                turntable.ThreadStep(1.0f/sampleRate);
                _sampleIndex = (turntable.loop / turntable.maxLoops) * SampleLength;
                Vector2 sample = _samples[Math.Clamp((int)_sampleIndex, 0, _samples.Length - 1)];
                // Ringpuffer für die Wellenform (Mono-Mix für Visualisierung)
                _waveformBuffer[_waveformIndex] = (sample.X + sample.Y) * 0.5f;
                _waveformIndex = (_waveformIndex + 1) % WaveformLength;
                _playback.PushFrame(sample);
                _speed = (float)((prevIndex - _sampleIndex) * sampleRate);
                samplesToWrite--;
                indexDelta = prevIndex - _sampleIndex;
            }
            _indexDifferencePlot[_indexDifferenceIndex] = (int)(_speed - sampleRate);
            _indexDifferenceIndex = (_indexDifferenceIndex + 1) % indexDifferenceLength;
            Thread.Sleep(delay);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Nur noch Visualisierung/Debugging!
        QueueRedraw();
    }

    private Font _defaultFont = ThemeDB.FallbackFont;
    public override void _Draw()
    {
        // Nur für Debugging

        // Wellenform zeichnen
        float midY = 150;
        float scaleX = 2.0f; // Abstand zwischen Linien
        float scaleY = 60.0f; // Amplitude-Skalierung

        Vector2 p1;
        Vector2 p2;

        for (int i = 0; i < WaveformLength - 1; i++)
        {
            int idx1 = (_waveformIndex + i) % WaveformLength;
            int idx2 = (_waveformIndex + i + 1) % WaveformLength;
            p1 = new Vector2(100 + i * scaleX, midY - _waveformBuffer[idx1] * scaleY);
            p2 = new Vector2(100 + (i + 1) * scaleX, midY - _waveformBuffer[idx2] * scaleY);
            DrawLine(p1, p2, Colors.Red, 2);
        }

        // Plot für FramesAvailable
        float plotBaseY = 250;
        float plotScaleX = 2.0f;
        float plotScaleY = 0.2f; // Skaliere je nach Buffergröße

        for (int i = 0; i < indexDifferenceLength - 1; i++)
        {
            int idx1 = (_indexDifferenceIndex + i) % indexDifferenceLength;
            int idx2 = (_indexDifferenceIndex + i + 1) % indexDifferenceLength;
            float y1 = plotBaseY - _indexDifferencePlot[idx1] * plotScaleY;
            float y2 = plotBaseY - _indexDifferencePlot[idx2] * plotScaleY;
            p1 = new Vector2(100 + i * plotScaleX, y1);
            p2 = new Vector2(100 + (i + 1) * plotScaleX, y2);
            DrawLine(p1, p2, new Color(0, 1, 0, 0.2f), 3);
        }

        p1 = new Vector2(100 + plotScaleX, plotBaseY);
        p2 = new Vector2(100 + indexDifferenceLength * plotScaleX, plotBaseY);
        DrawLine(p1, p2, new Color(1, 1, 1, 0.2f), 2);

        for (int i = 0; i < deltaLength - 1; i++)
        {
            int idx1 = (_deltaIndex + i) % deltaLength;
            int idx2 = (_deltaIndex + i + 1) % deltaLength;
            float y1 = plotBaseY - _deltaPlot[idx1] * plotScaleY;
            float y2 = plotBaseY - _deltaPlot[idx2] * plotScaleY;
            p1 = new Vector2(100 + i * plotScaleX, y1);
            p2 = new Vector2(100 + (i + 1) * plotScaleX, y2);
            DrawLine(p1, p2, new Color(0, 1, 1, 0.2f), 3);
        }

        // Text für Sample-Länge und aktuellen Index zeichnen
        string info = $"Sample Length: {_samples?.Length ?? 0:D7} | Index: {(int)_sampleIndex:D7} | Frames Available: {_playback.GetFramesAvailable():D5} | Skips: {_playback.GetSkips():D6} | Speed: {_speed:F3} | delta: {indexDelta:F5}";
        DrawString(_defaultFont, new Vector2(100, 30), info, HorizontalAlignment.Center);
    }

    // Play/Pause-Methoden
    public void Play()
    {
        if (!_player.Playing)
            _player.StreamPaused = false;
    }

    public void Pause()
    {
        if (_player.Playing)
            _player.StreamPaused = true;
    }

    public void JumpTo(float loop)
    {
        turntable.loop = loop;
        _sampleIndex = turntable.loop * turntable.maxLoops;
        _speed = turntable.currentSpeed / turntable.maxLoops * (SampleLength / sampleRate);
    }
}

// ----- TURNTABLE SIM -----

namespace Simulation
{
    public class Turntable
    {
        public float maxLoops = 670;
        public volatile float loop = 0;

        public bool motorRunning = true;
        private const float motorSpeed = 45f;
        private const float targetRunningSpeed = motorSpeed / 60f;

        public volatile float currentSpeed = 0f;
        public volatile float targetSpeed = 0f;
        private const float acceleration = 1.0f; // Umdrehungen pro Sekunde^2, anpassen nach Gefühl
        private const float drag = 0.8f;

        public Turntable(float songLength)
        {
            maxLoops = motorSpeed / 60 * songLength;
        }

        public void ThreadStep(double delta)
        {
            // Drag
            currentSpeed -= currentSpeed * drag * (float)delta;
            // Inertia
            if (MathF.Abs(currentSpeed - targetSpeed) > 0.001f)
            {
                float sign = MathF.Sign(targetSpeed - currentSpeed);
                currentSpeed += sign * acceleration * (float)delta;
                // Stabilisieren der Zielgeschwindigkeit
                if (sign != MathF.Sign(targetSpeed - currentSpeed))
                    currentSpeed = targetSpeed;
            }

            if (MathF.Abs(currentSpeed) > 0.00001f && motorRunning)
            {
                loop += currentSpeed * (float)delta;
                if (loop >= maxLoops)
                {
                    StopMotor();
                }
            }

            if (Mathf.Abs(currentSpeed) < 0.001f)
            {
                if (targetSpeed == 0)
                {
                    motorRunning = false;
                }
                else if (Math.Abs(targetSpeed) > 0)
                {
                    motorRunning = true;
                }
            }
        }

        public void StartMotor()
        {
            targetSpeed = targetRunningSpeed;
        }

        public void StopMotor()
        {
            targetSpeed = 0f;
        }

        public void ToggleMotor()
        {
            if (motorRunning)
                StopMotor();
            else
                StartMotor();
        }

        public void Rotate(float angle)
        {
            float loopDelta = angle / (Mathf.Pi * 2);
            loop += loopDelta;
        }
    }
}