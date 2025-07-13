using Godot;
using System;

public partial class AudioTester : AudioStreamPlayer
{
    [Export]
    public string DirectoryPath = "";
    [Export]
    public float GapInSeconds = 1.0f;

    private string[] _audioFiles;
    private int currentIndex = 0;
    private Timer gapTimer;

    public override void _Ready()
    {
        if (!string.IsNullOrEmpty(DirectoryPath))
        {
            var dir = DirAccess.Open(DirectoryPath);
            if (dir != null)
            {
                var files = new System.Collections.Generic.List<string>();
                dir.ListDirBegin();
                string fileName = dir.GetNext();
                while (fileName != "")
                {
                    if (!dir.CurrentIsDir() && IsAudioFile(fileName))
                        files.Add(System.IO.Path.Combine(DirectoryPath, fileName));
                    fileName = dir.GetNext();
                }
                dir.ListDirEnd();
                _audioFiles = files.ToArray();
            }
        }

        gapTimer = new Timer();
        gapTimer.WaitTime = GapInSeconds;
        gapTimer.OneShot = true;
        gapTimer.Timeout += OnGapTimerTimeout;
        AddChild(gapTimer);

        PlayNext();
    }

    private bool IsAudioFile(string fileName)
    {
        string ext = System.IO.Path.GetExtension(fileName).ToLower();
        return ext == ".wav" || ext == ".mp3";
    }

    private void PlayNext()
    {
        if (_audioFiles == null || _audioFiles.Length == 0)
            return;

        if (currentIndex >= _audioFiles.Length)
            currentIndex = 0;

        GD.Print($"Playing {_audioFiles[currentIndex]}");
        var stream = GD.Load<AudioStream>(_audioFiles[currentIndex]);
        if (stream != null)
        {
            Stream = stream;
            Play();
            Finished += OnAudioFinished;
        }
    }

    private void OnAudioFinished()
    {
        Finished -= OnAudioFinished;
        gapTimer.Start();
    }

    private void OnGapTimerTimeout()
    {
        currentIndex++;
        PlayNext();
    }
}
