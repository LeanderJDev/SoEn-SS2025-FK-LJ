using Godot;
using Musikspieler.Scripts.RecordView;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Musikspieler.Scripts
{
    public interface IItem
    {

    }

    public struct ItemsAddedEventArgs
    {
        public int startIndex;
        public int count;
    }

    public struct ItemsRemovedEventArgs
    {
        public int startIndex;
        public int count;
    }

    public interface IItemList
    {
        public int ItemCount { get; }
        public IItem this[int index] { get; }
        public event Action<ItemsAddedEventArgs> ItemsAdded;
        public event Action<ItemsRemovedEventArgs> ItemsRemoved;
        public ImmutableArray<IItem> GetAllItems();
        public IEnumerable<IItem> GetEnumerable();
        public bool AddItem(IItem item);
        public bool AddItems(List<IItem> items);
        public bool InsertItemAt(IItem item, int index);
        public bool InsertItemsAt(List<IItem> items, int index);
        public bool RemoveItem(IItem item);
        public bool RemoveItemAt(int index);
        public bool RemoveItemsAt(int startIndex, int count);

        //how many itemObjects could be added
        public int BufferSizeLeft { get; }
    }

    public interface IItemList<T> : IItemList where T : IItem
    {
        public new T this[int index] { get; }
        public new ImmutableArray<T> GetAllItems();
        public new IEnumerable<T> GetEnumerable();
        public bool AddItem(T item);
        public bool AddItems(List<T> items);
        public bool InsertItemAt(T item, int index);
        public bool InsertItemsAt(List<T> items, int index);
        public bool RemoveItem(T item);

        IItem IItemList.this[int index] => this[index];

        bool IItemList.AddItem(IItem item)
        {
            return AddItem((T)item);
        }
        bool IItemList.AddItems(List<IItem> items)
        {
            return AddItems(items.Cast<T>().ToList());
        }
        bool IItemList.InsertItemAt(IItem item, int index)
        {
            return InsertItemAt((T)item, index);
        }
        bool IItemList.InsertItemsAt(List<IItem> items, int index)
        {
            return InsertItemsAt(items.Cast<T>().ToList(), index);
        }
        bool IItemList.RemoveItem(IItem item)
        {
            return RemoveItem((T)item);
        }
        ImmutableArray<IItem> IItemList.GetAllItems()
        {
            return GetAllItems().Cast<IItem>().ToImmutableArray();
        }
        IEnumerable<IItem> IItemList.GetEnumerable()
        {
            return GetEnumerable().Cast<IItem>();
        }
    }

    public interface ISong : IItem
    {
        string Name { get; }
        string Album { get; }
        string Artist { get; }
        string MP3Path { get; }
        byte[] CoverData { get; }
        AudioStreamWav Audio { get; }
    }

    public interface IPlaylist : IItem, IItemList<ISong>
    {
        public string Name { get; }
    }

    public interface IPlaylistDirectory : IItemList<IPlaylist>
    {

    }

    public interface IAcceptsItemType<T> where T : IItem
    {

    }

    public interface IItemType<T> where T : IItem
    {

    }
}
