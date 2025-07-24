using Godot;
using System;

namespace Musikspieler.Scripts.RecordView
{
    public partial class GarbageBin : View
    {
        public static GarbageBin Instance { get; private set; }

        [Export] private CollisionShape3D viewBounds;

        public override event Action<ItemListChangedEventArgs> ObjectsChanged;

        public override CollisionShape3D BoundsShape => viewBounds;

        public override void _Ready()
        {
            base._Ready();
            if (Instance != null)
                throw new Exception("More than one GarbageBin exist.");
            Instance = this;
        }

        public override bool IsInitialized => true;

        public override ScrollViewContentContainer Container => throw new NotImplementedException();


        public override bool MoveItem(int index, View targetView) => false;     //man kann nichts rausnehmen

        public override ViewItem GrabItem(bool allowGrabChildren) => null;     //der Muelleimer gibt nie etwas her

        public override bool AcceptItem(ViewItem item, int? index)
        {
            //destroy item

            return true;
        }

        public override int GetViewIndex(ViewItem item)
        {
            throw new NotImplementedException();
        }

        public override void UpdateItemTransform(int index)
        {
            throw new NotImplementedException();
        }
    }
}
