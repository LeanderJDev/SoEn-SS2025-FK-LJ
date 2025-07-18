using System;
using Godot;
using Musikspieler.Scripts.Audio;

namespace Musikspieler.Scripts
{
    public class Song
    {
        public readonly string Name;
        public readonly string Album;
        public readonly string Artist;
        public readonly string MP3Path;

        private AudioStreamWav audio;
        public AudioStreamWav Audio => audio;

        public Song(
            string name,
            string album,
            string artist,
            string mp3Path,
            AudioStreamWav audioStream = null
        )
        {
            Name = name;
            Album = album;
            Artist = artist;
            MP3Path = mp3Path;
            audio = audioStream;
        }

        private void LoadAudio()
        {
            audio = MP3Loader.Load(MP3Path);
        }

        private void DisposeAudio()
        {
            audio.Dispose();
            audio = null;
        }
    }
}
