using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Musikspieler.Scripts
{
    public class Playlist : IPlaylist
    {
        private readonly List<ISong> songs = [];

        public int SongCount => songs.Count;

        public ImmutableArray<ISong> GetAllSongs()
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

        public event Action<SongsAddedEventArgs> SongsAdded = delegate { };
        public event Action<SongsRemovedEventArgs> SongsRemoved = delegate { };

        public ISong this[int index]
        {
            get { return songs[index]; }
        }

        public bool AddSong(ISong song)
        {
            if (song == null)
                return false;
            songs.Add(song);
            SongsAddedEventArgs args = new()
            {
                startIndex = songs.Count - 1,
                count = 1,
            };
            SongsAdded?.Invoke(args);
            return true;
        }

        public bool AddSongs(List<ISong> songList)
        {
            if (songs == null)
                return false;
            SongsAddedEventArgs args = new()
            {
                startIndex = songs.Count,
                count = songList.Count,
            };
            songs.AddRange(songList);
            SongsAdded?.Invoke(args);
            return true;
        }

        public bool InsertSongAt(ISong song, int index)
        {
            if (song == null || index >= SongCount || index < 0)
                return false;
            songs.Insert(index, song);
            SongsAddedEventArgs args = new()
            {
                startIndex = index,
                count = 1,
            };
            SongsAdded?.Invoke(args);
            return true;
        }

        public bool InsertSongsAt(List<ISong> songs, int index)
        {
            if (songs == null || index >= SongCount || index < 0 || songs.Count < 1)
                return false;
            songs.InsertRange(index, songs);
            SongsAddedEventArgs args = new()
            {
                startIndex = index,
                count = songs.Count,
            };
            SongsAdded?.Invoke(args);
            return true;
        }

        public bool RemoveSong(ISong song)
        {
            int index = songs.IndexOf(song);
            if (index < 0)
                return false;
            songs.RemoveAt(index);
            SongsRemovedEventArgs args = new()
            {
                startIndex = index,
                count = 1,
            };
            SongsRemoved?.Invoke(args);
            return true;
        }

        public bool RemoveSongAt(int index)
        {
            if (index >= SongCount || index < 0)
                return false;
            songs.RemoveAt(index);
            SongsRemovedEventArgs args = new()
            {
                startIndex = index,
                count = 1,
            };
            SongsRemoved?.Invoke(args);
            return true;
        }

        public bool RemoveSongsAt(int startIndex, int count)
        {
            if (startIndex + count >= SongCount || startIndex < 0)
                return false;
            songs.RemoveRange(startIndex, count);
            SongsRemovedEventArgs args = new()
            {
                startIndex = startIndex,
                count = count,
            };
            SongsRemoved?.Invoke(args);
            return true;
        }

        public int BufferSizeLeft => int.MaxValue - SongCount;
    }

    public interface IPlaylist
    {
        public int SongCount { get; }
        public ISong this[int index] { get; }
        public event Action<SongsAddedEventArgs> SongsAdded;
        public event Action<SongsRemovedEventArgs> SongsRemoved;
        public ImmutableArray<ISong> GetAllSongs();
        public IEnumerable<ISong> GetEnumerable();
        public bool AddSong(ISong song);
        public bool AddSongs(List<ISong> songs);
        public bool InsertSongAt(ISong song, int index);
        public bool InsertSongsAt(List<ISong> songs, int index);
        public bool RemoveSong(ISong song);
        public bool RemoveSongAt(int index);
        public bool RemoveSongsAt(int startIndex, int count);

        //how many songs could be added
        public int BufferSizeLeft { get; }
    }

    public struct SongsAddedEventArgs
    {
        public int startIndex;
        public int count;
    }

    public struct SongsRemovedEventArgs
    {
        public int startIndex;
        public int count;
    }
}