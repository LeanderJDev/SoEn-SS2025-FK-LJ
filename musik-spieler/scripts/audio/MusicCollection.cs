using Godot;
using System;

namespace Musikspieler.Scripts.Audio
{
	public sealed partial class MusicCollection : Node
	{
		public static MusicCollection Instance { get; private set; }

		private PlaylistDirectory _playlistDirectory = new PlaylistDirectory();
		public IPlaylistDirectory PlaylistDirectory => _playlistDirectory;


		private MusicCollection()
		{
			if (Instance != null)
				throw new Exception("There seem to be more than one GrabHandler in the Scene.");
			Instance = this;
		}


		public override void _Ready()
		{
			string MusicPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic);

			if (MusicPath == null)
			{
				GD.PrintErr("MusicDirectory: Could not find System.Environment.SpecialFolder.MyMusic");
				return;
			}

			Playlist wholeCollection = new Playlist("Whole Collection");

			wholeCollection.AddItems(SongLoader.LoadSongs(MusicPath));

			GD.Print(wholeCollection.ItemCount);
			GD.Print(wholeCollection[0].CoverData);

			_playlistDirectory.AddItem(wholeCollection);
		}
	}
}