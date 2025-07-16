using Godot;
using System.Collections.Generic;

namespace Musikspieler.Scripts
{
    public partial class RecordView : Node3D
    {
        [Export] Node3D _recordsContainer;
        [Export] CollisionShape3D recordViewBounds;

        public RecordContainer RecordsContainer => (RecordContainer)_recordsContainer;

        private int RecordCount => _playlist.SongCount;

        public ShaderMaterial CutoffMaterialInstance { get; private set; }

        private ViewPlaylist _playlist;
        public ViewPlaylist DisplayedPlaylist
        {
            get
            {
                return _playlist;
            }
            set
            {
                if (value == _playlist)
                    return;

                if (_playlist != null)
                {
                    _playlist.PlaylistChanged -= OnPlaylistChanged;
                }
                _playlist = value;
                if (_playlist != null)
                {
                    _playlist.PlaylistChanged += OnPlaylistChanged;
                }
            }
        }

        // Setup Settings
        private const float recordPackageWidth = 0.25f;     //als wie breit eine recordPackage behandelt wird
        private const float scrollAreaSize = 0.3f;          //wie groß der Bereich ist, in dem gescrollt werden kann (link und rechts, zw. 0 und 1)
        private const float flipThresholdOffset = -0.2f;    //um wie viel das Maus-spiel verschoben ist
        private const float flipThreshold = 1.7f;           //wie viel spiel die der Mauszeiger hat

        // User Settings
        public bool useAutoScroll = true;                   //ob, wenn die Maus an die Kanten des RecordViews kommt, automatsich gescrollt werden soll
        public float autoScrollSensitivity = 40f;           //wie schnell es auto-scrollt
        public float scrollSensitivity = 0.9f;              //wie schnell es mit der Maus scrollt


        public int GapIndex => (int)_centeredGapIndex + (RecordCount / 2);
        private float _centeredGapIndex;

        private Vector3 Bounds => ((BoxShape3D)recordViewBounds.Shape).Size;

        public IAnimationFunction FlickThroughRotationXAnimation { get; set; } = new BinaryFlickThroughRotationXAnimationFunction();
        public IAnimationFunction FlickThroughRotationYAnimation { get; set; } = new SubtleRotationYAnimationFunction();

        /// <summary>
        /// Eine Interface, um die Blätter-Animation zu bestimmen.
        /// </summary>
        /// Es wurde ein Interface einem Delegaten vorgezogen, damit die Animationen evtl. eigene Einstellungen speichern können, oder können interne Daten pro gerenderten Frame anpassen, z.B. für Physik-Modelle.
        /// Ein Interface ist wesentlich erweiterbarer.
        public interface IAnimationFunction
        {
            public float AnimationFunction(Vector2 relativeMousePos);
        }

        public struct BinaryFlickThroughRotationXAnimationFunction : IAnimationFunction
        {
            public readonly float AnimationFunction(Vector2 relativeMousePos)
            {
                float maxXAngle = Mathf.DegToRad(50);

                return relativeMousePos.Y < 0 ? maxXAngle * 0.4f : -maxXAngle;
            }
        }

        public struct LeaningFlickThroughRotationXAnimationFunction : IAnimationFunction
        {
            public readonly float AnimationFunction(Vector2 relativeMousePos)
            {
                float maxXAngle = Mathf.DegToRad(50);
                const float gapWidth = 4.0f;
                const float backSideOffset = 2.5f;

                if (relativeMousePos.Y < 0) relativeMousePos.Y -= backSideOffset;
                relativeMousePos.Y = Mathf.Clamp(relativeMousePos.Y, -gapWidth, gapWidth);
                return -0.5f * (Mathf.Cos(Mathf.Pi / gapWidth * relativeMousePos.Y) + 1) * Mathf.Sign(relativeMousePos.Y) * maxXAngle;
            }
        }

        public struct SubtleRotationYAnimationFunction : IAnimationFunction
        {
            public readonly float AnimationFunction(Vector2 relativeMousePos)
            {
                float maxYAngle = Mathf.DegToRad(6);

                Vector2 vNorm = relativeMousePos.Normalized();
                return Mathf.Min(Mathf.Abs(vNorm.X) / (100 * Mathf.Max(relativeMousePos.Length(), 0.3f)), maxYAngle) * Mathf.Sign(vNorm.Y * vNorm.X);
            }
        }


        public override void _Ready()
        {
            CutoffMaterialInstance = (ShaderMaterial)RecordPackage.defaultMaterial.Duplicate();

            //NUR FÜR TESTZWECKE
            GD.Print("RecordView created");
            List<ISong> songs = new(100);
            for (int i = 0; i < 100; i++)
            {
                songs.Add(new Song(Utility.RandomString(10)));
            }
            Playlist playlist = new();
            DisplayedPlaylist = new ViewPlaylist(playlist);
            DisplayedPlaylist.recordView = this;
            playlist.AddSongs(songs);
        }

        //containerMousePos auf der Boundary
        private Vector2? GetBoundaryMousePosition()
        {
            Mask<CollisionMask> mask = CollisionMask.RecordViewBoundary;

            //getroffen?
            if (!Utility.CameraRaycast(GetViewport().GetCamera3D(), mask, out var result))
                return null;

            //unseres getroffen?
            if ((Node)result["collider"] != recordViewBounds.GetParent())
                return null;

            Vector3 hitPos = (Vector3)result["position"];
            Vector3 localPos = recordViewBounds.GlobalTransform.AffineInverse() * hitPos;

            //raycast hit nützt uns nur, wenn wir die oberseite getroffen haben
            const float allowedInaccuracy = 0.05f;

            if (localPos.Y > Bounds.Y * (0.5f - allowedInaccuracy))
                return new Vector2(localPos.X, localPos.Z);
            else
                return null;
        }

        private void OnPlaylistChanged() { }

        private void Scroll(float lines)
        {
            RecordsContainer.Position -= new Vector3(0, 0, lines);
        }

        private void OnScrollInput(float lines)
        {
            Scroll(lines * scrollSensitivity);
        }

        private void OnLeftClickInput(bool pressed)
        {
            //das muss nicht hier, sondern in einem handler
            /*
            if (pressed)
            {
                var packageSlot = _playlist[(int)_centeredGapIndex];
                packageSlot.IsPending = true;
                packageSlot.IsDragged = true;
                _recordPackageObjects.RemoveAt((int)_centeredGapIndex);
                packageSlot.packageObject.Reparent(GetTree().Root, true);
                packageSlot.packageObject.Teleport();
                packageSlot.packageObject.MeshInstance.MaterialOverride = BaseRecordMaterial;

                //inform a handler that a record is dragged here
            }
            */
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
                {
                    if (mouseEvent.Pressed)
                        OnScrollInput(-1f);
                }
                else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
                {
                    if (mouseEvent.Pressed)
                        OnScrollInput(1f);
                }
                else if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    OnLeftClickInput(mouseEvent.Pressed);
                }
            }
        }

        private float lastMouseY;
        private float currentFlipOffset;

        public override void _Process(double delta)
        {
            base._Process(delta);
            Vector2? boundaryMousePos = GetBoundaryMousePosition();

            if (boundaryMousePos == null)
                return;

            Transform3D transform = RecordsContainer.GlobalTransform.AffineInverse() * recordViewBounds.GlobalTransform;
            Vector2 containerMousePos;
            if (useAutoScroll)
            {
                float normalizedBoundaryPos = boundaryMousePos.Value.Y / Bounds.Z;
                float scroll = Mathf.Max(Mathf.Abs(normalizedBoundaryPos) - (0.5f - scrollAreaSize), 0) * Mathf.Sign(normalizedBoundaryPos);
                Scroll(scroll * (float)delta * autoScrollSensitivity);
                float scrollArea = Bounds.Z * (0.5f - scrollAreaSize);
                float clamped = Mathf.Clamp(boundaryMousePos.Value.Y, -scrollArea, scrollArea);

                Vector3 localPos = transform * new Vector3(boundaryMousePos.Value.X, 0, clamped);
                containerMousePos = new(localPos.X, localPos.Z);
            }
            else
            {
                Vector3 localPos = transform * new Vector3(boundaryMousePos.Value.X, 0, boundaryMousePos.Value.Y);
                containerMousePos = new(localPos.X, localPos.Z);
            }

            float mouseZDelta = containerMousePos.Y - lastMouseY;
            currentFlipOffset = Mathf.Clamp(currentFlipOffset + mouseZDelta, -flipThreshold * 0.5f + flipThresholdOffset, flipThreshold * 0.5f + flipThresholdOffset);
            lastMouseY = containerMousePos.Y;
            _centeredGapIndex = (containerMousePos.Y - currentFlipOffset) / recordPackageWidth;

            for (int i = 0; i < _playlist.SongCount; i++)
            {
                var package = _playlist[i];
                Vector2 packageToMouse = new(containerMousePos.X - package.Position.X, _centeredGapIndex - (package.ViewIndex - RecordCount / 2));
                UpdatePackageTransforms(package, packageToMouse);
            }

            CutoffMaterialInstance.SetShaderParameter("box_transform", recordViewBounds.GlobalTransform);
            CutoffMaterialInstance.SetShaderParameter("box_size", ((BoxShape3D)recordViewBounds.Shape).Size);
        }

        private void UpdatePackageTransforms(RecordPackage package, Vector2 packageToMouse)
        {
            package.Position = new(0, 0, (package.ViewIndex - (RecordCount / 2)) * recordPackageWidth);

            float xRotation = FlickThroughRotationXAnimation.AnimationFunction(packageToMouse);
            float yRotation = FlickThroughRotationYAnimation.AnimationFunction(packageToMouse);

            package.Rotation = new Vector3(xRotation, yRotation, 0);
        }
    }
}