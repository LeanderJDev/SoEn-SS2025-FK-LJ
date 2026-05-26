using Godot;
using System;
using System.Collections.Generic;

namespace Musikspieler.Scripts.RecordView
{
    public abstract partial class View : StaticBody3D
    {
        //Nutzen, um mit Items im View zu interagieren.
        public abstract ViewItem GrabItem(bool allowGrabChildren);

        //Nutzen, um Items in einen anderen View zu verschieben.
        public abstract bool MoveItem(int index, View targetView);

        //Wird aufgerufen, ob ein eingehendes Item akzeptiert wird, z.B. durch MoveItem.
        public abstract bool AcceptItem(ViewItem item, int? index);
        public abstract bool IsInitialized { get; }
        public abstract CollisionShape3D BoundsShape { get; }
        public abstract int GetViewIndex(ViewItem item);

        /// <summary>
        /// Number of ContentItems in the View. Note that this does not contain Items within Items.
        /// </summary>
        public abstract int ItemCount { get; }

        /// <summary>
        /// Set to  -1 for no limit
        /// </summary>
        public abstract int MaxItemCount { get; }

        public abstract event Action<ItemListChangedEventArgs> ObjectsChanged;

        public struct ItemListChangedEventArgs
        {
            public readonly bool ViewChanged => changeToView != null;

            public List<ViewItem> itemsToChangeView;
            public View changeToView;
        }

        // nodes can request to get their transform targets set
        public abstract void UpdateItemTransform(int index);

        // a node that the items can parent to
        public abstract Node3D Container { get; }

        protected Mask<CollisionMask> mask;

        public bool IsUnderCursor
        {
            get => RaycastHandler.IsObjectUnderCursor(this);
        }
    }
}
