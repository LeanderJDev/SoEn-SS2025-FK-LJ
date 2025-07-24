using Godot;

namespace Musikspieler.Scripts.RecordView
{
    public partial class RecordPackage : ViewItemGeneric<ISong>
    {
        [Export]
        public ShaderMaterial coverImageMaterial;
        public static void Init() { }

        public override void _Ready()
        {
            base._Ready();
            if (displayedItem.CoverData != null)
            {
                GD.Print(displayedItem.CoverData.Length);
                Image image = new Image();
                image.LoadJpgFromBuffer(displayedItem.CoverData);
                ImageTexture texture = null;
                try
                {
                    texture = ImageTexture.CreateFromImage(image);
                }
                catch
                {
                    GD.Print($"Failed to load image {image.ResourceName}");
                }
                coverImageMaterial = (ShaderMaterial)coverImageMaterial.Duplicate();
                coverImageMaterial.SetShaderParameter("albedo_texture", texture);
                _meshInstance.SetSurfaceOverrideMaterial(1, coverImageMaterial);
            }
        }

        static RecordPackage()
        {
            ItemPrefab = GD.Load<PackedScene>("res://scenes/recordView/recordPackage.tscn");

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