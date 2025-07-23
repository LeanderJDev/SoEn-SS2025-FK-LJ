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

        public override ShaderMaterial LocalMaterial => ViewItemGeneric<IPlaylist>.DefaultMaterial;

        public Drawer _drawer;

        public override ViewItem GrabItem(bool allowGrabChildren)
        {
            return _drawer;
        }

        public override bool MoveItem(int index, View targetView)
        {
            if(targetView.AcceptItem(_drawer, null))
            {
                ObjectListChanged?.Invoke(new()
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

            if (_drawer != null && _drawer.displayedItem.Name.Equals(tempname))
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
            throw new NotImplementedException();
        }

        public override void UpdateItemTransform(int index)
        {
            throw new NotImplementedException();
        }
    }
}