using Godot;

namespace Musikspieler.Scripts.RecordView
{
    public abstract partial class View : Node3D
    {
        public abstract ViewItem Grab();
        public abstract bool MoveItem(int index, View targetView);
        public abstract bool IsItemListAssigned { get; }
    }
}
