using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musikspieler.Scripts
{
    public partial class RecordView : Node3D
    {
        [Export] RecordContainer recordsContainer;
        [Export] CollisionShape3D recordViewBounds;
        [Export] ShaderMaterial BaseRecordMaterial;

        private readonly List<RecordPackageSlot> recordPackageObjects = [];
        private int RecordCount => recordPackageObjects.Count;

        private struct RecordPackageSlot
        {
            public int index;                       //has to be updated if list changes
            public RecordPackage packageObject;     //can be null if outside the window! changes dynamically.
            public bool isPending;                  //if the package is currently in the air -> due to animation this is true for longer than isDragged
            public bool isDragged;                  //if the package is currently dragged around
            //public Song song;                       //muss eigentlich zu RecordPackage verschoben werden
        }

        private ShaderMaterial materialInstance;

        // Setup Settings
        private const float recordPackageWidth = 0.25f;     //als wie breit eine recordPackage behandelt wird
        private const float scrollAreaSize = 0.3f;          //wie groß der Bereich ist, in dem gescrollt werden kann (link und rechts, zw. 0 und 1)
        private const float flipThresholdOffset = -0.7f;    //um wie viel das Maus-spiel verschoben ist
        private const float flipThreshold = 1.7f;           //wie viel spiel die der Mauszeiger hat

        // User Settings
        public bool useAutoScroll = true;                   //ob, wenn die Maus an die Kanten des RecordViews kommt, automatsich gescrollt werden soll
        public float autoScrollSensitivity = 40f;           //wie schnell es auto-scrollt
        public float scrollSensitivity = 0.9f;             //wie schnell es mit der Maus scrollt


        public int GapIndex => (int)_gapIndex;
        private float _gapIndex;

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

            if (localPos.Y > Bounds.Y * (1 - allowedInaccuracy))
                return new Vector2(localPos.X, localPos.Z);
            else
                return null;
        }

        private void Scroll(float lines)
        {
            recordsContainer.Position -= new Vector3(0, 0, lines * scrollSensitivity);
        }

        private float lastMouseY;
        private float currentFlipOffset;

        public override void _Process(double delta)
        {
            Vector2? boundaryMousePos = GetBoundaryMousePosition();

            if (boundaryMousePos == null)
                return;

            Transform3D transform = recordsContainer.GlobalTransform.AffineInverse() * recordViewBounds.GlobalTransform;
            Vector2 containerMousePos;
            if (useAutoScroll)
            {
                float scroll = autoScrollSensitivity * (float)delta * (boundaryMousePos.Value.Y - Mathf.Sign(boundaryMousePos.Value.Y) * (0.5f - scrollAreaSize));
                Scroll(scroll);
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

            _gapIndex = (containerMousePos.Y - currentFlipOffset) / (recordPackageWidth * RecordCount);

            for (int i = 0; i < recordPackageObjects.Count; i++)
            {
                Vector3 packagePosition = recordPackageObjects[i].packageObject.Position;
                Vector2 packageToMouse = containerMousePos - new Vector2(packagePosition.X, packagePosition.Z);
                UpdatePackageTransforms(recordPackageObjects[i], packageToMouse);
            }
            
            materialInstance.SetShaderParameter("box_transform", recordViewBounds.GlobalTransform);
            materialInstance.SetShaderParameter("box_size", ((BoxShape3D)recordViewBounds.Shape).Size);
        }

        private void UpdatePackageTransforms(RecordPackageSlot packageSlot, Vector2 packageToMouse)
        {
            if (packageSlot.isPending)
            {
                if (!packageSlot.isDragged && packageSlot.packageObject.IsCloseToTargetPosition)
                {
                    packageSlot.isPending = false;
                    packageSlot.packageObject.MeshInstance.MaterialOverride = materialInstance;
                }
                return;
            }

            packageSlot.packageObject.Position = new(0, 0, (packageSlot.index - (recordPackageObjects.Count / 2)) * recordPackageWidth);

            float xRotation = FlickThroughRotationXAnimation.AnimationFunction(packageToMouse);
            float yRotation = FlickThroughRotationYAnimation.AnimationFunction(packageToMouse);

            packageSlot.packageObject.Rotation = new Vector3(xRotation, yRotation, 0);
        }
    }
}
