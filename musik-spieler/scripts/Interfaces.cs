using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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

	public interface IItemList<T> where T : IItem
	{
		public int ItemCount { get; }
		public T this[int index] { get; }
		public event Action<ItemsAddedEventArgs> ItemsAdded;
		public event Action<ItemsRemovedEventArgs> ItemsRemoved;
		public ImmutableArray<T> GetAllItems();
		public IEnumerable<T> GetEnumerable();
		public bool AddItem(T item);
		public bool AddItems(List<T> items);
		public bool InsertItemAt(T item, int index);
		public bool InsertItemsAt(List<T> items, int index);
		public bool RemoveItem(T item);
		public bool RemoveItemAt(int index);
		public bool RemoveItemsAt(int startIndex, int count);

		//how many itemObjects could be added
		public int BufferSizeLeft { get; }
	}

	public interface ISong : IItem
	{
		public string Name { get; }
		public float LengthInSeconds { get; }
	}

	public interface IPlaylist : IItem, IItemList<ISong>
	{

	}

	public interface IPlaylistDirectory : IItemList<IPlaylist>
	{

	}
}
