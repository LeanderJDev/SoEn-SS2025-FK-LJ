using Godot;
using System;
using System.Collections.Generic;

namespace Musikspieler.Scripts.RecordView
{
    public abstract partial class View : StaticBody3D
    {
        public abstract ViewItem GrabItem(bool allowGrabChildren);
        public abstract bool MoveItem(int index, View targetView);
        public abstract bool AcceptItem(ViewItem item, int? index);
        public abstract bool IsInitialized { get; }
        public abstract CollisionShape3D BoundsShape { get; }
        public abstract ShaderMaterial LocalMaterial { get; }
        public abstract int GetViewIndex(ViewItem item);

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
        public abstract ScrollViewContentContainer Container { get; }

        protected Mask<CollisionMask> mask;

        public bool IsUnderCursor
        {
            get => CheckIfViewUnderCursor(mask, out View view) && view == this;
        }

        protected bool CheckIfViewUnderCursor(Mask<CollisionMask> mask, out View view)
        {
            view = null;
            if (!Utility.CameraRaycast(GetViewport().GetCamera3D(), mask, out var result))
                return false;
            if (result == null || result.Count < 0)
                return false;
            if ((Node)result["collider"] is View hit)
            {
                view = hit;
                return true;
            }
            return false;
        }
    }
}
