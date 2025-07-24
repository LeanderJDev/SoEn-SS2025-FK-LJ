using Godot;
using System;
using TagLib;

namespace Musikspieler.Scripts.Audio
{
	public static class MP3Parser
	{
		public static Song Parse(string path)
		{
			var file = TagLib.File.Create(path);
			string title = file.Tag.Title ?? "Unbekannt";
            string album = file.Tag.Album ?? "Unbekannt";
            string artist = file.Tag.FirstPerformer ?? "Unbekannt";
			float length = (float)file.Properties.Duration.TotalSeconds;
			byte[] coverData = null;
            if (file.Tag.Pictures.Length > 0)
			{
				coverData = file.Tag.Pictures[0].Data.Data;
			}
            return new Song(title, album, artist, length, path, coverData);
		}
	}
}