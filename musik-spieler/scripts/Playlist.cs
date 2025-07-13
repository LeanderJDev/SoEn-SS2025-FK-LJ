using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Musikspieler.Scripts
{
    public class Playlist
    {
        private readonly List<Song> songs = [];

        public ImmutableArray<Song> GetAllSongs()
        {
            return songs.ToImmutableArray();
        }

        public IEnumerable<Song> GetEnumerable()
        {
            for (int i = 0; i < songs.Count; i++)
            {
                yield return songs[i];
            }
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

        public event Action<SongsAddedEventArgs> SongsAdded;
        public event Action<SongsRemovedEventArgs> SongsRemoved;

        public Song GetSongAtIndex(int index)
        {
            return songs[index];
        }

        public void AddSong(Song song)
        {
            songs.Add(song);
            SongsAddedEventArgs args = new()
            {
                startIndex = songs.Count - 1,
                count = 1,
            };
            SongsAdded?.Invoke(args);
        }

        public void InsertSongAt(Song song, int index)
        {
            songs.Insert(index, song);
            SongsAddedEventArgs args = new()
            {
                startIndex = index,
                count = 1,
            };
            SongsAdded?.Invoke(args);
        }

        public void InsertSongsAt(List<Song> songs, int index)
        {
            songs.InsertRange(index, songs);
            SongsAddedEventArgs args = new()
            {
                startIndex = index,
                count = songs.Count,
            };
            SongsAdded?.Invoke(args);
        }

        public void RemoveSong(Song song)
        {
            int index = songs.IndexOf(song);
            songs.RemoveAt(index);
            SongsRemovedEventArgs args = new()
            {
                startIndex = index,
                count = 1,
            };
            SongsRemoved?.Invoke(args);
        }

        public void RemoveSongAt(int index)
        {
            songs.RemoveAt(index);
            SongsRemovedEventArgs args = new()
            {
                startIndex = index,
                count = 1,
            };
            SongsRemoved?.Invoke(args);
        }

        public void RemoveSongsAt(int startIndex, int count)
        {
            songs.RemoveRange(startIndex, count);
            SongsRemovedEventArgs args = new()
            {
                startIndex = startIndex,
                count = count,
            };
            SongsRemoved?.Invoke(args);
        }

        public int SongCount => songs.Count;
    }
}
