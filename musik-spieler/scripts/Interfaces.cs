using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Musikspieler.Scripts
{
    public interface IContentItem
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
        public IContentItem this[int index] { get; }
        public event Action<ItemsAddedEventArgs> ItemsAdded;
        public event Action<ItemsRemovedEventArgs> ItemsRemoved;
        public ImmutableArray<IContentItem> GetAllItems();
        public IEnumerable<IContentItem> GetEnumerable();
        public bool AddItem(IContentItem item);
        public bool AddItems(List<IContentItem> items);
        public bool InsertItemAt(IContentItem item, int index);
        public bool InsertItemsAt(List<IContentItem> items, int index);
        public bool RemoveItem(IContentItem item);
        public bool RemoveItemAt(int index);
        public bool RemoveItemsAt(int startIndex, int count);

        //how many itemObjects could be added
        public int BufferSizeLeft { get; }
    }
}
