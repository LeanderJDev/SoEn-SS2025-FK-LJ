using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musikspieler.Scripts
{
    public class ViewPlaylist
    {
        private readonly IPlaylist playlist;

        private readonly List<RecordPackage> packages;

        private static readonly PackedScene recordPackagePrefab = GD.Load<PackedScene>("res://scenes/recordView/recordPackage.tscn");

        public int SongCount => packages.Count;

        public ViewPlaylist(IPlaylist playlist)
        {
            this.playlist = playlist;
            packages = new(playlist.SongCount);
            for (int i = 0; i < playlist.SongCount; i++)
            {
                packages.Add(InstantiateRecordPackage(playlist[i]));
            }
            playlist.SongsAdded += OnSongsAdded;
            playlist.SongsRemoved += OnSongsRemoved;
        }

        public RecordPackage this[int index]
        {
            get { return packages[index]; }
        }

        public RecordView recordView;

        public event Action PlaylistChanged = delegate { };

        private bool ignoreSongsAddedEvent = false;

        private void OnSongsAdded(SongsAddedEventArgs args)
        {
            if (ignoreSongsAddedEvent)
                return;

            List<RecordPackage> newPackages = new(args.count);
            for (int i = 0; i < args.count; i++)
            {
                //GD.Print($"instanciating recordpackage {i}");
                ISong song = playlist[args.startIndex + i];
                newPackages.Add(InstantiateRecordPackage(song));
            }
            packages.InsertRange(args.startIndex, newPackages);
            for (int i = 0; i < newPackages.Count; i++)
            {
                newPackages[i].Playlist = this;
            }
            PlaylistChanged?.Invoke();
        }

        private RecordPackage InstantiateRecordPackage(ISong song)
        {
            var package = (RecordPackage)recordPackagePrefab.Instantiate();
            package.song = song;
            recordView.RecordsContainer.AddChild(package);
            return package;
        }

        private void OnSongsRemoved(SongsRemovedEventArgs args)
        {
            packages.RemoveRange(args.startIndex, args.count);
            PlaylistChanged?.Invoke();
        }

        public int IndexOf(RecordPackage recordPackage)
        {
            return packages.IndexOf(recordPackage);
        }

        public void AddRecord(RecordPackage recordPackage)
        {
            ignoreSongsAddedEvent = true;
            if (playlist.AddSong(recordPackage.song))
            {
                packages.Add(recordPackage);
                PlaylistChanged?.Invoke();
            }
            ignoreSongsAddedEvent = false;
        }

        public void AddRecords(List<RecordPackage> recordPackages)
        {
            ignoreSongsAddedEvent = true;
            if (playlist.AddSongs(recordPackages.Select(x => x.song).ToList()))
            {
                packages.AddRange(recordPackages);
                PlaylistChanged?.Invoke();
            }
            ignoreSongsAddedEvent = false;
        }

        public void InsertRecordAt(RecordPackage recordPackage, int index)
        {
            ignoreSongsAddedEvent = true;
            if (playlist.InsertSongAt(recordPackage.song, index))
            {
                packages.Insert(index, recordPackage);
                PlaylistChanged?.Invoke();
            }
            ignoreSongsAddedEvent = false;
        }

        public void InsertRecordsAt(List<RecordPackage> recordPackages, int index)
        {
            ignoreSongsAddedEvent = true;
            if (playlist.InsertSongsAt(recordPackages.Select(x => x.song).ToList(), index))
            {
                packages.InsertRange(index, recordPackages);
                PlaylistChanged?.Invoke();
            }
            ignoreSongsAddedEvent = false;
        }

        public RecordPackage RemoveRecord(RecordPackage recordPackage)
        {
            if (recordPackage == null)
                return null;

            if (!packages.Contains(recordPackage))
                return null;

            if (playlist.RemoveSong(recordPackage.song))
                return recordPackage;
            else
                return null;
            //it will get removed from the list in the event callback
        }

        public RecordPackage RemoveRecordAt(int index)
        {
            if (playlist.RemoveSongAt(index))
                return packages[index];
            else
                return null;
            //it will get removed from the list in the event callback
        }

        public List<RecordPackage> RemoveRecordsAt(int startIndex, int count)
        {
            if (playlist.RemoveSongsAt(startIndex, count))
                return packages.GetRange(startIndex, count);
            else
                return null;
            //they will get removed from the list in the event callback
        }
    }

}
