using Godot;
using System;

namespace Musikspieler.Scripts.Audio
{
    public static class SongLoader
    {
        public static List<Song> LoadSongs(string path)
        {
			var songs = new List<Song>();
			if (!Directory.Exists(path))
				return songs;

			var files = Directory.GetFiles(path, "*.mp3");
			foreach (var file in files)
			{
				try
				{
					var song = MP3Parser.Parse(file);
					if (song != null)
						songs.Add(song);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"Failed to load song from {file}: {ex.Message}");
				}
			}
			return songs;
        }
    }
}
