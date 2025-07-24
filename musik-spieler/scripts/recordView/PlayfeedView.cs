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

        [Export] private Node3D _container;

        public override ScrollViewContentContainer Container => (ScrollViewContentContainer)_container;

        public override ShaderMaterial LocalMaterial => ViewItemGeneric<IPlaylist>.DefaultMaterial;

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
                    itemsToChangeView = [_drawer]
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

            if (_drawer != null && _drawer.displayedItem.Name.Equals(tempname))
            {
                GD.Print("automatisch den alten Drawer zurückzumoven ist noch nicht implementiert.");
                return false;
            }
            if (item is Drawer drawer)
            {
                GD.Print("set drawer in playfeedview");
                _drawer = drawer;
                return true;
            }
            if (item is RecordPackage recordPackage)
            {
                _drawer = new()
                {
                    displayedItem = new Playlist(tempname)
                };
                _drawer.displayedItem.AddItem(recordPackage.displayedItem);
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