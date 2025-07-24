using Godot;
using System.Collections.Generic;
using TagLib.Ape;

namespace Musikspieler.Scripts.RecordView
{
    public interface IItemAndView
    {
        View ChildView { get; }
    }

    public partial class Drawer : ViewItem, IItemAndView
    {
        [Export] private RecordView _recordView;

        [Export] private CollisionObject3D _handle;

        public RecordView RecordView => _recordView;

        View IItemAndView.ChildView => _recordView;

        private static PackedScene _prefab;

        public IPlaylist Playlist { get; protected set; }

        public override IItem DisplayedItem
        {
            get => Playlist;
            protected set
            {
                if (value is IPlaylist playlist)
                    this.Playlist = playlist;
                else GD.PrintErr("A Drawer can only hold an IItem of type IPlaylist!");
            }
        }

        public static Drawer InstantiateAndAssign(View view, int playlistIndex)
        {
            if (!view.IsCompatibleWith(typeof(Drawer)))
            {
                GD.PrintErr($"incompatible {view.GetType()}, {PrintDict(View.TypeCompatibilites)}");
                return null;
            }
            IPlaylist displayedItem = (IPlaylist)view[playlistIndex].DisplayedItem;
            var item = _prefab.Instantiate<Drawer>();
            item.Playlist = displayedItem;
            item.View = view;
            return item;
        }

        private static string PrintDict<A, B>(Dictionary<A, B> dict)
        {
            string s = "";
            foreach (var item in dict)
            {
                s += item.ToString();
            }
            return s;
        }

        public static Drawer InstatiateAndAssign(View view, int index)
        {
            if (view is not IAcceptsItemType<IPlaylist> acceptedView)
                return null;
            Drawer drawer = _prefab.Instantiate<Drawer>();
            drawer.View = view;
            drawer.ViewIndex = index;
            drawer._meshInstance.MaterialOverride = view.LocalMaterial;
            return drawer;
        }

        private bool _selected;
        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                ((DrawerView)View).SetSelected(ViewIndex, _selected);
            }
        }

        public static void Init() { }

        public override void _Ready()
        {
            RecordView.ItemList = Playlist;
            base._Ready();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
                {
                    if (RaycastHandler.IsObjectUnderCursor(_handle))
                        Selected = !Selected;
                }
            }
            base._Input(@event);
        }

        static Drawer()
        {
            _prefab = GD.Load<PackedScene>("res://scenes/recordView/drawer.tscn");
            DefaultMaterial = GD.Load<ShaderMaterial>("res://graphics/defaultRecordPackageMaterial.tres");

            const float PositionSmoothTime = 0.10f;
            const float PositionMaxSpeed = 50f;
            const float RotationSmoothTime = 0.07f;
            const float RotationMaxSpeed = 40f;
            const float ScaleSmoothTime = 0.10f;
            const float ScaleMaxSpeed = 20f;

            ObjectTypeSmoothDamp = new(PositionSmoothTime, PositionMaxSpeed, RotationSmoothTime, RotationMaxSpeed, ScaleSmoothTime, ScaleMaxSpeed);
        }
    }
}
