using Godot;
using System;
using System.Collections.Generic;
using Musikspieler.Scripts.RecordView;
using Musikspieler.Scripts.Audio;

namespace Musikspieler.Scripts.UI
{
    public partial class RecordAccepter : View
    {
        // Container for accepted items (could be the turntable itself or a child node)
        [Export] private CollisionShape3D viewBounds;
		public override CollisionShape3D BoundsShape => viewBounds;

		[Export]
		private TurntableAudioManager turntableAudioManager;

        // List to keep track of accepted items
		private ViewItem currentItem;

        // Event for item changes
        public override event Action<ItemListChangedEventArgs> ObjectsChanged;


        public override bool IsInitialized => true;

		// Das sollte eigentlich nicht im View generalisiert sein
		public override Node3D Container => this;

        public override int GetViewIndex(ViewItem item)
        {
            return 0;
        }

        public override ViewItem GrabItem(bool allowGrabChildren)
        {
			GD.Print("RecordAccepter: GrabItem", currentItem);
            // Only one record can be accepted at a time (for a turntable)
			return currentItem;

        }

        public override bool MoveItem(int index, View targetView)
        {
			GD.Print("RecordAccepter: MoveItem");
            if (currentItem == null)
				return false;
			var item = currentItem;
			currentItem = null;
            ObjectsChanged?.Invoke(new ItemListChangedEventArgs
            {
                itemsToChangeView = new List<ViewItem> { item },
                changeToView = targetView
            });
            return true;
        }

        public override bool AcceptItem(ViewItem item, int? index)
		{
			GD.Print("RecordAccepter: AcceptItem");
			if (item is RecordPackage recordPackage)
			{
				// Only one record at a time
				if (currentItem != null)
					return false;

				currentItem = item;

				turntableAudioManager.SetSong(recordPackage.displayedItem);

				ObjectsChanged?.Invoke(new ItemListChangedEventArgs
				{
					itemsToChangeView = new List<ViewItem> { item },
					changeToView = this
				});
				return true;
			}
			return false;
		}

        public override void UpdateItemTransform(int index)
        {
            if (currentItem == null)
                return;
            currentItem.GlobalTransform = this.GlobalTransform;
        }
    }
}

