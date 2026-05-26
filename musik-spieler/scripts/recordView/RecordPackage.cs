using Godot;

namespace Musikspieler.Scripts.RecordView
{
    public partial class RecordPackage : ViewItem
    {
        [Export]
        public ShaderMaterial coverImageMaterial;

        public Song DisplayedSong => (Song)displayedItem;
        public static void Init() { }

        public override void _Ready()
        {
            base._Ready();
            if (displayedItem is Song song && song.CoverData != null)
            {
                Image image = new Image();
                image.LoadJpgFromBuffer(song.CoverData);
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
            else
            {
                GD.PrintErr("A RecordPackage cannot display any other Content other than a Song. A cover image is required.");
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