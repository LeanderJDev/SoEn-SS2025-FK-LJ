using Godot;
using System;
using System.Threading;

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
    public int SampleLength => _samples.Length;

    private Thread _audioThread;
    private volatile bool _audioThreadRunning = false;

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

        _audioThreadRunning = true;
        _audioThread = new Thread(AudioThreadLoop);
        _audioThread.Start();
    }

    public override void _ExitTree()
    {
        _audioThreadRunning = false;
        _audioThread?.Join();
    }

    private void AudioThreadLoop()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        double lastTime = sw.Elapsed.TotalSeconds;
        while (_audioThreadRunning)
        {
            double now = sw.Elapsed.TotalSeconds;
            double delta = now - lastTime;
            lastTime = now;
            while (_playback.GetFramesAvailable() > 0)
            {
                Vector2 sample = _samples[Math.Clamp((int)_sampleIndex, 0, _samples.Length - 1)];
                // Ringpuffer für die Wellenform (Mono-Mix für Visualisierung)
                _waveformBuffer[_waveformIndex] = (sample.X + sample.Y) * 0.5f;
                _waveformIndex = (_waveformIndex + 1) % WaveformLength;
                _playback.PushFrame(sample);
                _sampleIndex += _speed;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Nur noch Visualisierung/Debugging!
        QueueRedraw();
    }

    //turntableSpeed ist hier nur, weil wenn wir eh springen, können wir auch gleich die Geschwindigkeiten syncen, das fällt ja dann nicht auf
    public void JumpTo(float turntablePos, float turntableSpeed)
    {
        _sampleIndex = turntablePos * SampleLength;
        _speed = turntableSpeed * (SampleLength / sampleRate);
    }

    float turntableSpeed = 0;

    public void FillBuffer(float delta, float turntableSpeed, float turntablePos)
    {
        float currentPos = _sampleIndex / SampleLength;
        float turntableStep = turntableSpeed * delta;
        float targetPos = turntablePos + turntableStep;
        float targetStep = targetPos - currentPos;
        float newSpeed = targetStep * (SampleLength / (float)sampleRate);

        //versuchen, aufzuholen bzw. weich zu syncronisieren
        _speed = newSpeed;
        this.turntableSpeed = turntableSpeed;

        //_speed = turntableSpeed * (SampleLength / sampleRate);
        // Berechne Menge an zu schreibenden Samples aus Daten des Turntables
        //samplesToWrite = (int)(delta * sampleRate * Math.Abs(_speed));
        _indexDifferencePlot[_indexDifferenceIndex] = (int)(_sampleIndex - turntablePos * SampleLength);
        _indexDifferenceIndex = (_indexDifferenceIndex + 1) % indexDifferenceLength;
        //_sampleIndex = turntablePos * SampleLength;
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

        // Text für Sample-Länge und aktuellen Index zeichnen
        string info = $"Sample Length: {_samples?.Length ?? 0:D7} | Index: {(int)_sampleIndex:D7} | Frames Available: {_playback.GetFramesAvailable():D5} | Skips: {_playback.GetSkips():D6} | Speed: {_speed:F3} | TSpeed: {turntableSpeed}";
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
}
