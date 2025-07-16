using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts
{
    public partial class RecordView : Node3D
    {
        private readonly List<RecordPackage> currentlyDraggedPackages = [];
        private readonly List<RecordPackageSlot> recordPackageObjects = [];

        private int RecordCount => recordPackageObjects.Count;

        [Export] CollisionShape3D mousePlane;
        [Export] Node3D recordsContainer;
        [Export] Node3D movableContainer;
        [Export] CollisionShape3D recordViewBounds;
        [Export] ShaderMaterial BaseMaterial;

        private ShaderMaterial instancedMaterial;
        private int lastPickupGapIndex = -1;
        private const float recordPackageWidth = 0.25f;
        public float gapIndex;
        private float unconsumedScrollDelta;

        private float Margin => ((BoxShape3D)recordViewBounds.Shape).Size.Z * 0.5f;
        private Vector3 Bounds => ((BoxShape3D)recordViewBounds.Shape).Size;
        private float ContainerLength => ((BoxShape3D)mousePlane.Shape).Size.Z;

        private const float scrollAreaSize = 0.3f;          //wie groß der Bereich ist, in dem gescrollt werden kann (link und rechts, zw. 0 und 1)
        private const float flipThresholdOffset = -0.7f;    //um wie viel das Maus-spiel verschoben ist
        private const float flipThreshold = 1.7f;           //wie viel spiel die der Mauszeiger hat
        public float autoScrollSensitivity = 40f;           //wie schnell es auto-scrollt
        public float scrollSensitivity = -0.9f;             //wie schnell es mit der Maus scrollt

        private const float movableContainerSmoothTime = 0.1f;  //Konsistenz des Scrollens
        private const float movableContainerMaxSpeed = 40f;

        private float lastMouseY = 0;                       //on the mousePlane
        private float currentFlipOffset = 0;                //wo liegen wie im mouse-spiel?

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

        private float movableContainerVelocity;
        private float _movableContainerTarget;
        private float MovableContainerTarget
        {
            get => _movableContainerTarget;
            set
            {
                float halfSize = ContainerLength * 0.5f - Margin;
                value = Math.Clamp(value, -halfSize, halfSize);
                _movableContainerTarget = value;
            }
        }

        private struct RecordPackageSlot
        {
            public int index;                   //has to be updated if list changes
            public RecordPackage packageObject; //can be null if outside the window! changes dynamically.
            public Song song;                   //muss eigentlich zu RecordPackage verschoben werden
        }

        //just for testing
        private class Song(string name)
        {
            public readonly string name = name;
        }

        public override void _Ready()
        {
            base._Ready();
            RequestReady();         //so that _ready() will be called again for the next instance of this class
            Init();
        }

        private void Init()
        {
            //just for testing
            List<Song> playlistSongs = new(100);
            for (int i = 0; i < 100; i++)
            {
                playlistSongs.Add(new Song(Utility.RandomString(10)));
            }

            if (mousePlane == null)
                throw new Exception("no collision shape");
            if (recordsContainer == null)
                throw new Exception("no records container");

            // Material instanziieren
            instancedMaterial = (ShaderMaterial)BaseMaterial.Duplicate();
            PackedScene recordPrefab = GD.Load<PackedScene>("res://record_package.tscn");
            for (int i = 0; i < playlistSongs.Count; i++)
            {
                RecordPackage record = (RecordPackage)recordPrefab.Instantiate();
                record.Name = $"RecordPackage_{i}";
                recordsContainer.AddChild(record);       //adds the instantiated object to the scene, makes it visible
                recordPackageObjects.Add(new RecordPackageSlot() { index = i, packageObject = record, song = playlistSongs[i] });
                record.MeshInstance.MaterialOverride = instancedMaterial;
            }

            SetPlaylistSize();
        }

        private void SetPlaylistSize()
        {
            const float clickPlaneWidth = 2.5f;

            float size = recordPackageWidth * recordPackageObjects.Count + Margin * 2;
            ((BoxShape3D)mousePlane.Shape).Size = new Vector3(clickPlaneWidth, 0.01f, size);
            recordsContainer.Position = new Vector3(0, 0, -size * 0.5f);

            for (int i = 0; i < recordPackageObjects.Count; i++)
            {
                recordPackageObjects[i] = new RecordPackageSlot()
                {
                    packageObject = recordPackageObjects[i].packageObject,
                    song = recordPackageObjects[i].song,
                    index = i,
                };
            }
        }

        private Dictionary CameraRaycast(uint mask)
        {
            var camera = GetViewport().GetCamera3D();

            if (camera == null)
            {
                GD.Print("no cam");
                return null;
            }

            Vector2 mousePos = GetViewport().GetMousePosition();

            Vector3 from = camera.ProjectRayOrigin(mousePos);
            Vector3 to = from + camera.ProjectRayNormal(mousePos) * 1000;

            var spaceState = GetWorld3D().DirectSpaceState;
            return spaceState.IntersectRay(new PhysicsRayQueryParameters3D
            {
                From = from,
                To = to,
                CollisionMask = mask
            });
        }

        //mousePos auf der mousePlane, die mitscrollt
        public Vector3? GetDraggingMousePos()
        {
            var result = CameraRaycast(4);

            if (result == null)
                return null;

            if (result.Count > 0)
            {
                return (Vector3)result["position"];
            }
            else return null;
        }

        //mousePos auf der Boundary
        private Vector2? GetRelativeMousePos()
        {
            var result = CameraRaycast(8);

            if (result == null || result.Count == 0)
                return null;

            result = CameraRaycast(2);

            if (result == null)
                return null;

            if (result.Count > 0 && (Node3D)result["collider"] == mousePlane.GetParent())
            {
                Vector3 hitPos = (Vector3)result["position"];

                Vector3 localPos = mousePlane.GlobalTransform.AffineInverse() * hitPos;
                return new Vector2(localPos.X, localPos.Z + ContainerLength * 0.5f);
            }
            else return null;
        }
        /*
        private static float LeaningAnimationFunction(Vector2 v)
        {
            float maxXAngle = Mathf.DegToRad(50);
            const float gapWidth = 4.0f;
            const float backSideOffset = 2.5f;

            if (v.Y < 0) v.Y -= backSideOffset;
            v.Y = Mathf.Clamp(v.Y, -gapWidth, gapWidth);
            return -0.5f * (Mathf.Cos(Mathf.Pi / gapWidth * v.Y) + 1) * Mathf.Sign(v.Y) * maxXAngle;
        }

        private static float BinaryAnimationFunction(Vector2 dstToMouse)
        {
            float maxXAngle = Mathf.DegToRad(50);

            return dstToMouse.Y < 0 ? maxXAngle * 0.4f : -maxXAngle;
        }

        private static float VerticalRotationAnimationFunction(Vector2 dstToMouse)
        {
            float maxYAngle = Mathf.DegToRad(6);

            Vector2 vNorm = dstToMouse.Normalized();
            return Mathf.Min(Mathf.Abs(vNorm.X) / (100 * Mathf.Max(dstToMouse.Length(), 0.3f)), maxYAngle) * Mathf.Sign(vNorm.Y * vNorm.X);
        }
        */
        //Man könnte das Updaten des Zielzustands auch entkoppelter mit Events lösen, jedoch macht das wenig Sinn, da sie sowieso von hier gemanaged sind, und keine eigenständigen Objekte sind.
        //Wären sie das, könnt evtl. nicht sichergestellt werden, 
        /// <summary>
        /// Die Schallplatten-Packungen haben einen TransformTarget, also einen Zielzustand (Translation und RotationParameters) den sie erreichen sollen. Hier wird dieser Zustand neu gesetzt.
        /// </summary>
        private void UpdatePackageTransformTargets(RecordPackageSlot packageSlot, float mousePosX)
        {
            if (currentlyDraggedPackages.Contains(packageSlot.packageObject))
            {
                //vllt. noch passend drehen hier
                return;
            }

            packageSlot.packageObject.Position = new(0, 0, Margin + packageSlot.index * recordPackageWidth);

            //wieder zurück rechnen wirkt sinnlos, aber damit funktioniert es immer noch, wenn gapIndex mal von extern gesetzt wird
            float gapIndexToZPos = gapIndex / RecordCount * (ContainerLength - Margin * 2) + Margin;

            Vector2 packagePos = new Vector2(packageSlot.packageObject.Position.X, packageSlot.packageObject.Position.Z);
            Vector2 packageToMouse = new Vector2(mousePosX, gapIndexToZPos) - packagePos;

            float xRotation = FlickThroughRotationXAnimation.AnimationFunction(packageToMouse);
            float yRotation = FlickThroughRotationYAnimation.AnimationFunction(packageToMouse);

            packageSlot.packageObject.Rotation = new Vector3(xRotation, yRotation, 0);
        }

        public override void _Process(double delta)
        {
            Vector2? mousePos = GetRelativeMousePos();

            //mousePos so manipulieren, dass es nur bis zum rand der scroll-Bereiche umblättert
            if (mousePos.HasValue)
            {
                //TODO das hier ist ganz umständlich und eklig, das muss besser gehen!!

                //transform space
                Vector2 size = new Vector2(((BoxShape3D)recordViewBounds.Shape).Size.X, ((BoxShape3D)recordViewBounds.Shape).Size.Y) * 2f;
                Vector3 globalPos = mousePlane.GlobalTransform * new Vector3(mousePos.Value.X, 0, mousePos.Value.Y - ((BoxShape3D)mousePlane.Shape).Size.Z * 0.5f);
                Vector3 localPos = recordViewBounds.GlobalTransform.AffineInverse() * globalPos;
                Vector2 newMousePos = new Vector2(localPos.X, localPos.Z) / size;

                if (newMousePos.Y < -0.5f + scrollAreaSize || newMousePos.Y > 0.5f - scrollAreaSize)
                {
                    unconsumedScrollDelta += autoScrollSensitivity * (float)delta * (newMousePos.Y - Mathf.Sign(newMousePos.Y) * (0.5f - scrollAreaSize));
                    newMousePos = new Vector2(newMousePos.X, Mathf.Clamp(newMousePos.Y, -0.5f + scrollAreaSize, 0.5f - scrollAreaSize)) * size;

                    globalPos = recordViewBounds.GlobalTransform * new Vector3(newMousePos.X, 0, newMousePos.Y);
                    localPos = mousePlane.GlobalTransform.AffineInverse() * globalPos;
                    mousePos = new Vector2(localPos.X, localPos.Z + ((BoxShape3D)mousePlane.Shape).Size.Z * 0.5f);
                }
            }

            if (mousePos.HasValue)
            {
                float mouseZDelta = mousePos.Value.Y - lastMouseY;
                currentFlipOffset = Mathf.Clamp(currentFlipOffset + mouseZDelta, -flipThreshold * 0.5f + flipThresholdOffset, flipThreshold * 0.5f + flipThresholdOffset);
                lastMouseY = mousePos.Value.Y;

                mousePos = new Vector2(mousePos.Value.X, mousePos.Value.Y - currentFlipOffset);
            }

            //nur gapIndex anpassen, wenn die Maus auch über den packages hovered
            if (mousePos.HasValue)
            {
                gapIndex = (mousePos.Value.Y - Margin) / (ContainerLength - Margin * 2) * RecordCount;
            }

            if (mousePos.HasValue)
            {
                for (int i = 0; i < recordPackageObjects.Count; i++)
                {
                    UpdatePackageTransformTargets(recordPackageObjects[i], mousePos.Value.X);
                }
            }

            MovableContainerTarget += unconsumedScrollDelta * scrollSensitivity;

            float newZ = SmoothDamp.Step(movableContainer.Position.Z, MovableContainerTarget, ref movableContainerVelocity, movableContainerSmoothTime, movableContainerMaxSpeed, (float)delta);
            movableContainer.Position = new Vector3(movableContainer.Position.X, movableContainer.Position.Y, newZ);

            unconsumedScrollDelta = 0;

            if (currentlyDraggedPackages.Count > 0)
            {
                Vector3? globalMousePos = GetDraggingMousePos();

                if (!globalMousePos.HasValue)
                    return;

                foreach (var package in currentlyDraggedPackages)
                {
                    package.Position = globalMousePos.Value;
                }
            }
            instancedMaterial.SetShaderParameter("box_transform", recordViewBounds.GlobalTransform);
            instancedMaterial.SetShaderParameter("box_size", ((BoxShape3D)recordViewBounds.Shape).Size);

            if (pendingPackages.Count > 0)
            {
                var package = pendingPackages[0];
                if ((package.Position - package.Position).LengthSquared() < 0.4f)
                {
                    pendingPackages.Remove(package);
                    package.MeshInstance.MaterialOverride = instancedMaterial;
                }
            }
        }


        /// <summary>
        /// packages that are being put in the playlist, but have get close enough to so we can change the shader bounds.
        /// </summary>
        private readonly List<RecordPackage> pendingPackages = [];

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                //mouseEvent.Pressed == true, da Godot scrollen auch als drücken und loslassen interpretiert, d.h. wie bekommen sonst alles doppelt 0_0
                if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed)
                {
                    if (GetRelativeMousePos() != null)
                        unconsumedScrollDelta--;
                }
                else if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed)
                {
                    if (GetRelativeMousePos() != null)
                        unconsumedScrollDelta++;
                }
                else if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (mouseEvent.Pressed)
                    {
                        if (gapIndex < 0)
                            return;

                        lastPickupGapIndex = (int)gapIndex;

                        var package = recordPackageObjects[(int)gapIndex].packageObject;
                        currentlyDraggedPackages.Add(package);
                        recordPackageObjects.RemoveAt((int)gapIndex);
                        package.Reparent(GetTree().Root, true);
                        package.Teleport(package.Position, package.Rotation);
                        package.MeshInstance.MaterialOverride = BaseMaterial;
                    }
                    else
                    {
                        if (currentlyDraggedPackages.Count == 0)
                            return;

                        int gapIndex = (int)this.gapIndex + 1;

                        if (gapIndex < 0)
                        {
                            gapIndex = lastPickupGapIndex;
                        }

                        var package = currentlyDraggedPackages.First();
                        recordPackageObjects.Insert(gapIndex, new RecordPackageSlot() { packageObject = package, index = gapIndex });
                        currentlyDraggedPackages.Remove(package);
                        package.Reparent(recordsContainer, true);
                        package.Teleport(package.Position, package.Rotation);
                        SetPlaylistSize();
                        pendingPackages.Add(package);
                    }
                }
            }
        }
    }
}
