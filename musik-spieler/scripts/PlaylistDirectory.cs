using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Musikspieler.Scripts.RecordView
{
    //Eine Klasse, die alle Playlists enthält.
    public class PlaylistDirectory : IPlaylistDirectory
    {
        public IPlaylist this[int index] => throw new NotImplementedException();

        public int ItemCount => throw new NotImplementedException();

        public int BufferSizeLeft => throw new NotImplementedException();

        public event Action<IItemList<IPlaylist>.ItemsAddedEventArgs> ItemsAdded;
        public event Action<IItemList<IPlaylist>.ItemsRemovedEventArgs> ItemsRemoved;

        public bool AddItem(IPlaylist item)
        {
            throw new NotImplementedException();
        }

        public bool AddItems(List<IPlaylist> items)
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<IPlaylist> GetAllItems()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlaylist> GetEnumerable()
        {
            throw new NotImplementedException();
        }

        public bool InsertItemAt(IPlaylist item, int index)
        {
            throw new NotImplementedException();
        }

        public bool InsertItemsAt(List<IPlaylist> items, int index)
        {
            throw new NotImplementedException();
        }

        public bool RemoveItem(IPlaylist item)
        {
            throw new NotImplementedException();
        }

        public bool RemoveItemAt(int index)
        {
            throw new NotImplementedException();
        }

        public bool RemoveItemsAt(int startIndex, int count)
        {
            throw new NotImplementedException();
        }
    }
}
