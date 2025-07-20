using Godot;
using System;

namespace Musikspieler.Scripts.RecordView
{
    public partial class GarbageBin : View
    {
        public static GarbageBin Instance { get; private set; }

        [Export] private CollisionShape3D viewBounds;
        public override CollisionShape3D BoundsShape => viewBounds;

        public override void _Ready()
        {
            base._Ready();
            if (Instance != null)
                throw new Exception("More than one GarbageBin exist.");
            Instance = this;
        }

        public override bool IsItemListAssigned => true;

        public override ViewItem Grab() => null;     //der Muelleimer gibt nie etwas her

        public override bool MoveItem(int index, View targetView) => false;  //man kann nichts rausnehmen
    }
}
