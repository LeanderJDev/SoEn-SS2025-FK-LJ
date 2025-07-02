using Godot;
using System;
using System.Threading;

public partial class AudioManager : Node2D
{
    [Export] public AudioStream Sample; // Typ geändert!
    private AudioStreamGenerator _generator;
    private AudioStreamGeneratorPlayback _playback;
    private Vector2[] _samples; // Stereo: X = links, Y = rechts
    private int _sampleIndex = 0;
    private float _speed = 1.0f; // 1.0 = normal, -1.0 = rückwärts
    private bool _hold = false;
    private float _currentSample = 0.0f; // Für Visualisierung

    private const int WaveformLength = 200;
    private float[] _waveformBuffer = new float[WaveformLength];
    private int _waveformIndex = 0;

    private AudioStreamPlayer _player;

    private const int FramesPlotLength = 200;
    private int[] _framesAvailablePlot = new int[FramesPlotLength];
    private int _framesPlotIndex = 0;

    private Thread _audioThread;
    private bool _audioThreadRunning = false;

    public override void _Ready()
    {
        // AudioStreamGenerator initialisieren
        _generator = new AudioStreamGenerator();
        _generator.MixRate = 44100;
        _generator.BufferLength = 0.5f;
        _player = new AudioStreamPlayer();
        AddChild(_player);
        _player.Stream = _generator;
        _player.VolumeLinear = 0.1f;
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

        // 1323000 für 30s 44100kHz
        _audioThreadRunning = true;
        _audioThread = new Thread(AudioThreadLoop);
        _audioThread.Priority = ThreadPriority.Highest;
        _audioThread.Start();
    }

    private void AudioThreadLoop()
    {
        while (_audioThreadRunning)
        {
            int frames = _playback.GetFramesAvailable();
            for (int i = 0; i < frames; i++)
            {
                Vector2 sample = _samples[Math.Clamp(_sampleIndex, 0, _samples.Length - 1)];
                _playback.PushFrame(sample);
                if (!_hold && _sampleIndex < _samples.Length)
                    _sampleIndex += (int)_speed;
            }
            Thread.Sleep(5); // Kurze Pause, damit der Thread nicht 100% CPU frisst
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Nach dem Pushen der Samples:
        _framesAvailablePlot[_framesPlotIndex] = _playback.GetFramesAvailable();
        _framesPlotIndex = (_framesPlotIndex + 1) % FramesPlotLength;
        while (_playback.GetFramesAvailable() > 0)
        {
            Vector2 sample = _samples[Math.Clamp(_sampleIndex, 0, _samples.Length - 1)];
            _currentSample = (sample.X + sample.Y) * 0.5f;

            // Ringpuffer für die Wellenform (Mono-Mix für Visualisierung)
            _waveformBuffer[_waveformIndex] = _currentSample;
            _waveformIndex = (_waveformIndex + 1) % WaveformLength;

            _playback.PushFrame(sample); // Stereo!

            if (!_hold && _sampleIndex < _samples.Length)
                _sampleIndex += (int)_speed;
        }
        QueueRedraw();
    }

    private Font _defaultFont = ThemeDB.FallbackFont;
    public override void _Draw()
    {
        // Wellenform zeichnen
        float midY = 150;
        float scaleX = 2.0f; // Abstand zwischen Linien
        float scaleY = 60.0f; // Amplitude-Skalierung

        for (int i = 0; i < WaveformLength - 1; i++)
        {
            int idx1 = (_waveformIndex + i) % WaveformLength;
            int idx2 = (_waveformIndex + i + 1) % WaveformLength;
            Vector2 p1 = new Vector2(100 + i * scaleX, midY - _waveformBuffer[idx1] * scaleY);
            Vector2 p2 = new Vector2(100 + (i + 1) * scaleX, midY - _waveformBuffer[idx2] * scaleY);
            DrawLine(p1, p2, Colors.Red, 2);
        }

        // Plot für FramesAvailable
        float plotBaseY = 250;
        float plotScaleX = 2.0f;
        float plotScaleY = 0.2f; // Skaliere je nach Buffergröße

        for (int i = 0; i < FramesPlotLength - 1; i++)
        {
            int idx1 = (_framesPlotIndex + i) % FramesPlotLength;
            int idx2 = (_framesPlotIndex + i + 1) % FramesPlotLength;
            float y1 = plotBaseY - _framesAvailablePlot[idx1] * plotScaleY;
            float y2 = plotBaseY - _framesAvailablePlot[idx2] * plotScaleY;
            Vector2 p1 = new Vector2(100 + i * plotScaleX, y1);
            Vector2 p2 = new Vector2(100 + (i + 1) * plotScaleX, y2);
            DrawLine(p1, p2, new Color(0, 1, 0, 0.2f), 2);
        }

        // Text für Sample-Länge und aktuellen Index zeichnen
        string info = $"Sample Length: {_samples?.Length ?? 0} | Index: {_sampleIndex} | Frames Available: {_playback.GetFramesAvailable()}";
        DrawString(_defaultFont, new Vector2(100, plotBaseY + 30), info, HorizontalAlignment.Center);
    }

    // Methoden für Interaktivität
    public void SetSpeed(float speed) => _speed = speed; // Für Scratching
    public void Hold(bool hold) => _hold = hold;         // Für Halten
    public void SetPosition(float percent)
    {
        _sampleIndex = (int)(percent * _samples.Length);
    }

    // Play/Pause-Methoden
    public void Play()
    {
        GD.Print($"{_sampleIndex} {_speed} {_player.Playing} {_playback.GetFramesAvailable()}");
        if (!_player.Playing)
            _player.StreamPaused = false;
    }

    public void Pause()
    {
        GD.Print($"{_sampleIndex} {_speed} {_player.Playing} {_playback.GetFramesAvailable()}");
        if (_player.Playing)
            _player.StreamPaused = true;
    }

    public void TogglePlayPause()
    {
        if (_player.Playing)
            _player.StreamPaused = true;
        else
            _player.StreamPaused = false;
    }

    public int SampleLength => _samples.Length;
    public void SetSampleIndex(int index)
    {
        _sampleIndex = Mathf.Clamp(index, 0, _samples.Length - 1);
    }

    public int GetSampleIndex()
    {
        return _sampleIndex;
    }

    public override void _ExitTree()
    {
        _audioThreadRunning = false;
        _audioThread?.Join();
        base._ExitTree();
    }
}
