using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Musikspieler.Scripts
{
	//Eine Klasse, die alle Playlists enthält.
	public class MusicCollection : IPlaylistDirectory
	{
		private readonly List<IPlaylist> _playlists;

		public IPlaylist this[int index] => _playlists[index];

		public int ItemCount => _playlists.Count;

		public int BufferSizeLeft => int.MaxValue - ItemCount;

		public event Action<ItemsAddedEventArgs> ItemsAdded;
		public event Action<ItemsRemovedEventArgs> ItemsRemoved;

		public MusicCollection()
		{
			_playlists = [];
		}

		public MusicCollection(List<IPlaylist> playlists)
		{
			playlists ??= [];
			_playlists = playlists;
		}

		public ImmutableArray<IPlaylist> GetAllItems() => [.. _playlists];

		public IEnumerable<IPlaylist> GetEnumerable()
		{
			for (int i = 0; i < _playlists.Count; i++)
			{
				yield return _playlists[i];
			}
		}

		public bool AddItem(IPlaylist song)
		{
			if (song == null)
				return false;
			_playlists.Add(song);
			ItemsAddedEventArgs args = new()
			{
				startIndex = _playlists.Count - 1,
				count = 1,
			};
			ItemsAdded?.Invoke(args);
			return true;
		}

		public bool AddItems(List<IPlaylist> songList)
		{
			if (_playlists == null)
				return false;
			ItemsAddedEventArgs args = new()
			{
				startIndex = _playlists.Count,
				count = songList.Count,
			};
			_playlists.AddRange(songList);
			ItemsAdded?.Invoke(args);
			return true;
		}

		public bool InsertItemAt(IPlaylist song, int index)
		{
			if (song == null || index >= ItemCount || index < 0)
				return false;
			_playlists.Insert(index, song);
			ItemsAddedEventArgs args = new()
			{
				startIndex = index,
				count = 1,
			};
			ItemsAdded?.Invoke(args);
			return true;
		}

		public bool InsertItemsAt(List<IPlaylist> _playlist, int index)
		{
			if (_playlist == null || index >= ItemCount || index < 0 || _playlist.Count < 1)
				return false;
			_playlist.InsertRange(index, _playlist);
			ItemsAddedEventArgs args = new()
			{
				startIndex = index,
				count = _playlist.Count,
			};
			ItemsAdded?.Invoke(args);
			return true;
		}

		public bool RemoveItem(IPlaylist song)
		{
			int index = _playlists.IndexOf(song);
			if (index < 0)
				return false;
			_playlists.RemoveAt(index);
			ItemsRemovedEventArgs args = new()
			{
				startIndex = index,
				count = 1,
			};
			ItemsRemoved?.Invoke(args);
			return true;
		}

		public bool RemoveItemAt(int index)
		{
			if (index >= ItemCount || index < 0)
				return false;
			_playlists.RemoveAt(index);
			ItemsRemovedEventArgs args = new()
			{
				startIndex = index,
				count = 1,
			};
			ItemsRemoved?.Invoke(args);
			return true;
		}

		public bool RemoveItemsAt(int startIndex, int count)
		{
			if (startIndex + count >= ItemCount || startIndex < 0)
				return false;
			_playlists.RemoveRange(startIndex, count);
			ItemsRemovedEventArgs args = new()
			{
				startIndex = startIndex,
				count = count,
			};
			ItemsRemoved?.Invoke(args);
			return true;
		}
	}
}
