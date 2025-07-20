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

        public RecordView RecordView => _recordView;

        View IItemAndView.ChildView => _recordView;

        public static void Init() { }

        public override void _Ready()
        {
            GD.Print("okaaay");
            RecordView.ItemList = displayedItem;
            base._Ready();
        }

        static Drawer()
        {
            GD.Print("Drawer static constructor");

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
