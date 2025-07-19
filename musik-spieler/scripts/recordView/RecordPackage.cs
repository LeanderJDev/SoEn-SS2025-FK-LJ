using Godot;

namespace Musikspieler.Scripts.RecordView
{
    public partial class RecordPackage : ViewItem<ISong>
    {
        public static void Init() { }
        
        static RecordPackage()
        {
            RecordPackagePrefab = GD.Load<PackedScene>("res://scenes/recordView/recordPackage.tscn");
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