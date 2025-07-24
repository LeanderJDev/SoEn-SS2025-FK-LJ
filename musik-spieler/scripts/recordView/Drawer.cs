using Godot;

namespace Musikspieler.Scripts.RecordView
{
    public interface IItemAndView
    {
        public View ChildView { get; }
    }

    public partial class Drawer : ViewItemGeneric<IPlaylist>, IItemAndView
    {
        [Export] private RecordView _recordView;

        [Export] private CollisionObject3D _handle;

        public RecordView RecordView => _recordView;

        View IItemAndView.ChildView => _recordView;

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
            RecordView.ItemList = displayedItem;
            base._Ready();
        }

        public override bool Move(View targetView)
        {
            GD.Print("Drawer: Move");
            return base.Move(targetView);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
                {
                    if (RaycastHandler.IsObjectUnderCursor(_handle))
                        Selected = !Selected;
                }
            }
            base._Input(@event);
        }

        static Drawer()
        {
            ItemPrefab = GD.Load<PackedScene>("res://scenes/recordView/drawer.tscn");
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
