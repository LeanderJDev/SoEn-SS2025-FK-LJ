using System;
using Godot;
using Musikspieler.Scripts.Audio;

namespace Musikspieler.Scripts
{
    public class Song : ISong
    {
        // Backing fields
        private readonly string _name;
        private readonly string _album;
        private readonly string _artist;
        private readonly string _mp3Path;
        private AudioStreamWav _audio;
        private byte[] _coverData;

        // Properties
        public string Name => _name;
        public string Album => _album;
        public string Artist => _artist;
        public string MP3Path => _mp3Path;
        public AudioStreamWav Audio => _audio;
        public byte[] CoverData => _coverData;

        public Song(
            string name,
            string album,
            string artist,
            string mp3Path,
            byte[] coverData = null,
            AudioStreamWav audioStream = null
        )
        {
            _name = name;
            _album = album;
            _artist = artist;
            _mp3Path = mp3Path;
            _coverData = coverData;
            _audio = audioStream;
        }

        public override string ToString()
        {
            return $"{nameof(Song)}: Name='{_name}', Album='{_album}', Artist='{_artist}', MP3Path='{_mp3Path}'";
        }

        private void LoadAudio()
        {
            _audio = MP3Loader.Load(_mp3Path);
        }

        private void DisposeAudio()
        {
            _audio?.Dispose();
            _audio = null;
        }
    }

    public interface ISong
    {
        string Name { get; }
        string Album { get; }
        string Artist { get; }
        string MP3Path { get; }
        AudioStreamWav Audio { get; }
    }
}
