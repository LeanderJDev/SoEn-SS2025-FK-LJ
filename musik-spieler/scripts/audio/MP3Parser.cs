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

            return new Song(title, album, artist, path);
		}
	}
}