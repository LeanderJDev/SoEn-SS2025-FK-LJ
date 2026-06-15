using Godot;
using System;

namespace Musikspieler.Scripts.Audio
{
	public sealed partial class MusicCollection : Node
	{
		public static MusicCollection Instance { get; private set; }

		public PlaylistDirectory PlaylistDirectory { get; private set; } = new PlaylistDirectory();

		private MusicCollection()
		{
			if (Instance != null)
				throw new Exception("There seem to be more than one GrabHandler in the Scene.");
			Instance = this;
		}

		public Action CollectionChanged = delegate { };

		public override void _Ready()
		{
			GD.Print("Start loading Songs...");

			string MusicPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic);

			if (MusicPath == null)
			{
				GD.PrintErr("MusicDirectory: Could not find System.Environment.SpecialFolder.MyMusic");
				return;
			}

			Playlist wholeCollection = new("Whole Collection");

			wholeCollection.AddItems(SongLoader.LoadSongs(MusicPath));

			GD.Print($"Songs Loaded: {wholeCollection.ItemCount}");

			PlaylistDirectory.AddItem(wholeCollection);

			CollectionChanged?.Invoke();
		}
	}
}
