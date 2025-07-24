using Godot;
using System;

namespace Musikspieler.Scripts.RecordView
{
    public partial class PlayfeedView : View
    {
        private bool _isInitalized;
        public override bool IsInitialized => _isInitalized;

        [Export] private CollisionShape3D _collisionShape;

        public override event Action<ItemListChangedEventArgs> ObjectsChanged;

        public override CollisionShape3D BoundsShape => _collisionShape;

        public override ScrollViewContentContainer Container => _drawer == null ? null : _drawer.RecordView.Container;

        public override int ItemCount => _drawer == null ? 0 : _drawer.RecordView.ItemCount;

        public override ViewItem this[int index]
        {
            get
            {
                if (_drawer != null)
                    return _drawer.RecordView[index];
                else return _drawer;
            }
        }

        public Drawer _drawer;

        public override ViewItem GrabItem(bool allowGrabChildren)
        {
            return _drawer;
        }

        public Vector3 DrawerPos;

        public override bool MoveItem(int index, View targetView)
        {
            if(targetView.AcceptItem(_drawer, null))
            {
                ObjectsChanged?.Invoke(new()
                {
                    changeToView = targetView,
                    items = [_drawer]
                });
                _drawer = null;
                return true;
            }

            return false;
        }

        public override void _Ready()
        {
            _isInitalized = true;
            base._Ready();
        }

        public override bool AcceptItem(ViewItem item, int? index)
        {
            const string tempname = "temp";

            if (_drawer != null && ((IPlaylist)_drawer.DisplayedItem).Name.Equals(tempname))
            {
                GD.Print("automatisch den alten Drawer zurückzumoven ist noch nicht implementiert.");
                return false;
            }
            if (item is Drawer drawer)
            {
                _drawer = drawer;
                return false;
            }
            if (item is RecordPackage recordPackage)
            {
                _drawer = Drawer.InstantiateAndAssign(this, 0);
                _drawer.Playlist.AddItem(recordPackage.Song);
                return false;
            }
            return false;
        }

        public override int GetViewIndex(ViewItem item)
        {
            return 0;
        }

        public override void UpdateItemTransform(int index)
        {
            _drawer.Position = DrawerPos;
            _drawer.Rotation = Vector3.Zero;
            _drawer.Scale = Vector3.One;
        }
    }
}