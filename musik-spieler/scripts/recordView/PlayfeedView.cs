using Godot;
using System;

namespace Musikspieler.Scripts.RecordView
{
    public partial class PlayfeedView : View
    {
        private bool _isInitalized;
        public override bool IsInitialized => _isInitalized;

        [Export] private CollisionShape3D _collisionShape;

        public override event Action<ItemListChangedEventArgs> ObjectListChanged;

        public override CollisionShape3D BoundsShape => _collisionShape;

        public override ScrollViewContentContainer Container => throw new NotImplementedException();

        public override ShaderMaterial LocalMaterial => throw new NotImplementedException();

        public override ViewItem GrabItem(bool allowGrabChildren)
        {
            throw new NotImplementedException();
        }

        public override bool MoveItem(int index, View targetView)
        {
            throw new NotImplementedException();
        }

        public override void _Ready()
        {
            _isInitalized = true;
            base._Ready();
        }

        public override bool AcceptItem(ViewItem item, int? index)
        {
            throw new NotImplementedException();
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