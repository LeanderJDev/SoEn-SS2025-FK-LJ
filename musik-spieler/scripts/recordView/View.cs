using Godot;

namespace Musikspieler.Scripts.RecordView
{
    public abstract partial class View : StaticBody3D
    {
        public abstract ViewItem Grab();
        public abstract bool MoveItem(int index, View targetView);
        public abstract bool IsItemListAssigned { get; }
        public abstract CollisionShape3D BoundsShape { get; }
    }
}
