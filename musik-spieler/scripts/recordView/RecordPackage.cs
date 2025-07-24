using Godot;

namespace Musikspieler.Scripts.RecordView
{
    public partial class RecordPackage : ViewItem, IItemType<ISong>
    {
        public static void Init() { }

        public override IItem DisplayedItem
        {
            get => Song;
            protected set
            {
                if (value is ISong song)
                    Song = song;
                else GD.PrintErr("A RecordPackage can only hold an IItem of type ISong!");
            }
        }

        private static PackedScene _prefab;

        public static RecordPackage InstantiateAndAssign(View view, int index)
        {
            RecordPackage package = _prefab.Instantiate<RecordPackage>();
            package.View = view;
            package.ViewIndex = index;
            return package;
        }


        public ISong Song { get; protected set; }

        public override void _Ready()
        {

            base._Ready();
        }

        static RecordPackage()
        {
            _prefab = GD.Load<PackedScene>("res://scenes/recordView/recordPackage.tscn");
            DefaultMaterial = GD.Load<ShaderMaterial>("res://graphics/defaultRecordPackageMaterial.tres");

            const float PositionSmoothTime = 0.10f;
            const float PositionMaxSpeed = 20f;
            const float RotationSmoothTime = 0.07f;
            const float RotationMaxSpeed = 40f;
            const float ScaleSmoothTime = 0.10f;
            const float ScaleMaxSpeed = 20f;

            ObjectTypeSmoothDamp = new(PositionSmoothTime, PositionMaxSpeed, RotationSmoothTime, RotationMaxSpeed, ScaleSmoothTime, ScaleMaxSpeed);
        }
    }
}