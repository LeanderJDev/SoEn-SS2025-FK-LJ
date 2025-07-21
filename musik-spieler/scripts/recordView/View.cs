using Godot;

namespace Musikspieler.Scripts.RecordView
{
    public abstract partial class View : StaticBody3D
    {
        public abstract ViewItem GrabItem();
        public abstract ViewItem AutoGrabItem();
        public abstract bool MoveItem(int index, View targetView);
        public abstract bool IsItemListAssigned { get; }
        public abstract CollisionShape3D BoundsShape { get; }
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
            GD.Print("o_o");
            if (result == null || result.Count < 0)
                return false;
            GD.Print("0_0");
            if ((Node)result["collider"] is View hit)
            {
                view = hit;
                GD.Print("O_O");
                return true;
            }
            return false;
        }
    }
}
