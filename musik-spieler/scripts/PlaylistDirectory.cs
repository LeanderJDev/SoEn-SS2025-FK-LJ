using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TagLib.Ape;

namespace Musikspieler.Scripts
{
    //Eine Klasse, die alle Playlists enthält.
    public class PlaylistDirectory : IItemList
    {
        private readonly List<Playlist> _playlists;

        public Playlist this[int index] => _playlists[index];
        public Playlist this[string name] => _playlists.FirstOrDefault(x => x.Name == name);

        public int ItemCount => _playlists.Count;

        public int BufferSizeLeft => int.MaxValue - ItemCount;

        public event Action<ItemsAddedEventArgs> ItemsAdded;
        public event Action<ItemsRemovedEventArgs> ItemsRemoved;

        public PlaylistDirectory()
        {
            _playlists = [];
        }

        public PlaylistDirectory(List<Playlist> playlists)
        {
            playlists ??= [];
            _playlists = playlists;
        }

        public ImmutableArray<Playlist> GetAllItems() => [.. _playlists];

        public IEnumerable<IContentItem> GetEnumerable()
        {
            for (int i = 0; i < _playlists.Count; i++)
            {
                yield return _playlists[i];
            }
        }

        public bool AddItem(IContentItem item)
        {
            if (item is Playlist playlist)
                return AddItem(playlist);
            else return false;
        }

        public bool AddItem(Playlist playlist)
        {
            if (playlist == null)
                return false;
            _playlists.Add(playlist);
            ItemsAddedEventArgs args = new()
            {
                startIndex = _playlists.Count - 1,
                count = 1,
            };
            ItemsAdded?.Invoke(args);
            return true;
        }

        public bool AddItems(List<IContentItem> items)
        {
            List<Playlist> playlists = new();
            foreach (IContentItem item in items)
            {
                if (item is Playlist playlist)
                    playlists.Add(playlist);
                else return false;
            }
            return AddItems(playlists);
        }

        public bool AddItems(List<Playlist> playlistList)
        {
            if (_playlists == null)
                return false;
            ItemsAddedEventArgs args = new()
            {
                startIndex = _playlists.Count,
                count = playlistList.Count,
            };
            _playlists.AddRange(playlistList);
            ItemsAdded?.Invoke(args);
            return true;
        }

        public bool InsertItemAt(Playlist playlist, int index)
        {
            if (playlist == null || index >= ItemCount || index < 0)
                return false;
            _playlists.Insert(index, playlist);
            ItemsAddedEventArgs args = new()
            {
                startIndex = index,
                count = 1,
            };
            ItemsAdded?.Invoke(args);
            return true;
        }

        public bool InsertItemsAt(List<Playlist> _playlistList, int index)
        {
            if (_playlistList == null || index >= ItemCount || index < 0 || _playlistList.Count < 1)
                return false;
            _playlistList.InsertRange(index, _playlistList);
            ItemsAddedEventArgs args = new()
            {
                startIndex = index,
                count = _playlistList.Count,
            };
            ItemsAdded?.Invoke(args);
            return true;
        }

        public bool RemoveItem(Playlist playlist)
        {
            int index = _playlists.IndexOf(playlist);
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
