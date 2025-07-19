using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Musikspieler.Scripts
{
    public class Playlist : IPlaylist
    {
        private readonly List<ISong> songs = [];

        public int ItemCount => songs.Count;

        public ImmutableArray<ISong> GetAllItems()
        {
            return songs.ToImmutableArray();
        }

        public IEnumerable<ISong> GetEnumerable()
        {
            for (int i = 0; i < songs.Count; i++)
            {
                yield return songs[i];
            }
        }

        public event Action<IPlaylist.ItemsAddedEventArgs> ItemsAdded = delegate { };
        public event Action<IPlaylist.ItemsRemovedEventArgs> ItemsRemoved = delegate { };

        public ISong this[int index]
        {
            get { return songs[index]; }
        }

        public bool AddItem(ISong song)
        {
            if (song == null)
                return false;
            songs.Add(song);
            IPlaylist.ItemsAddedEventArgs args = new()
            {
                startIndex = songs.Count - 1,
                count = 1,
            };
            ItemsAdded?.Invoke(args);
            return true;
        }

        public bool AddItems(List<ISong> songList)
        {
            if (songs == null)
                return false;
            IPlaylist.ItemsAddedEventArgs args = new()
            {
                startIndex = songs.Count,
                count = songList.Count,
            };
            songs.AddRange(songList);
            ItemsAdded?.Invoke(args);
            return true;
        }

        public bool InsertItemAt(ISong song, int index)
        {
            if (song == null || index >= ItemCount || index < 0)
                return false;
            songs.Insert(index, song);
            IPlaylist.ItemsAddedEventArgs args = new()
            {
                startIndex = index,
                count = 1,
            };
            ItemsAdded?.Invoke(args);
            return true;
        }

        public bool InsertItemsAt(List<ISong> songs, int index)
        {
            if (songs == null || index >= ItemCount || index < 0 || songs.Count < 1)
                return false;
            songs.InsertRange(index, songs);
            IPlaylist.ItemsAddedEventArgs args = new()
            {
                startIndex = index,
                count = songs.Count,
            };
            ItemsAdded?.Invoke(args);
            return true;
        }

        public bool RemoveItem(ISong song)
        {
            int index = songs.IndexOf(song);
            if (index < 0)
                return false;
            songs.RemoveAt(index);
            IPlaylist.ItemsRemovedEventArgs args = new()
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
            songs.RemoveAt(index);
            IPlaylist.ItemsRemovedEventArgs args = new()
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
            songs.RemoveRange(startIndex, count);
            IPlaylist.ItemsRemovedEventArgs args = new()
            {
                startIndex = startIndex,
                count = count,
            };
            ItemsRemoved?.Invoke(args);
            return true;
        }

        public int BufferSizeLeft => int.MaxValue - ItemCount;
    }
}