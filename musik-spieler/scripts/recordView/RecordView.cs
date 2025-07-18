using Godot;
using System;
using System.Collections.Generic;

namespace Musikspieler.Scripts.RecordView
{
    public partial class RecordView : Node3D
    {
        [Export] Node3D _recordsContainer;
        [Export] CollisionShape3D recordViewBounds;

        public RecordContainer RecordsContainer => (RecordContainer)_recordsContainer;

        private int RecordCount => packages.Count;

        public ShaderMaterial CutoffMaterialInstance { get; private set; }

        private IPlaylist _playlist;
        public IPlaylist Playlist
        {
            get => _playlist;
            set
            {
                if (_playlist != null)
                {
                    _playlist.SongsAdded -= OnSongsAdded;
                    _playlist.SongsRemoved -= OnSongsRemoved;
                    for (int i = 0; i < RecordCount; i++)
                    {
                        packages[i].QueueFree();
                    }
                    packages = null;
                }
                _playlist = value;
                if (_playlist != null)
                {
                    packages = new(_playlist.SongCount);
                    for (int i = 0; i < _playlist.SongCount; i++)
                    {
                        packages.Add(RecordPackage.InstantiateAndAssign(this, i));
                    }
                    _playlist.SongsAdded += OnSongsAdded;
                    _playlist.SongsRemoved += OnSongsRemoved;
                }
            }
        }

        private List<RecordPackage> packages;

        public int SongCount => packages.Count;

        public RecordPackage this[int index]
        {
            get { return packages[index]; }
        }

        public event Action<PlaylistChangedEventArgs> PlaylistChanged = delegate { };

        public struct PlaylistChangedEventArgs
        {
            public readonly bool RecordsRemoved => changeToView != null;
            public readonly bool RecordsAdded => changeToView == null;

            public List<RecordPackage> packages;
            public RecordView changeToView;
        }

        private bool ignoreSongsAddedEvent = false;
        private bool ignoreSongsRemovedEvent = false;

        private void OnSongsAdded(SongsAddedEventArgs args)
        {
            if (ignoreSongsAddedEvent)
                return;

            List<RecordPackage> newPackages = new(args.count);
            for (int i = 0; i < args.count; i++)
            {
                RecordPackage package = RecordPackage.InstantiateAndAssign(this, i);
                newPackages.Add(package);
                RecordsContainer.AddChild(package);
            }
            if (args.startIndex >= RecordCount)
                packages.AddRange(newPackages);
            else
                packages.InsertRange(args.startIndex, newPackages);
            PlaylistChanged?.Invoke(new()
            {
                packages = newPackages,
                changeToView = null,
            });
        }

        private void OnSongsRemoved(SongsRemovedEventArgs args)
        {
            if (ignoreSongsRemovedEvent)
                return;

            List<RecordPackage> packagesToDelete = new(args.count);
            for (int i = 0; i < args.count; i++)
            {
                packagesToDelete.Add(packages[args.startIndex + i]);
                //package.QueueFree(); //macht jetzt der garbage bin
            }
            packages.RemoveRange(args.startIndex, args.count);
            PlaylistChanged?.Invoke(new()
            {
                packages = packagesToDelete,
                changeToView = RecordGrabHandler.Instance.GarbageBin,
            });
        }

        public int IndexOf(RecordPackage recordPackage)
        {
            return packages.IndexOf(recordPackage);
        }

        /// <summary>
        /// Move the open Record to another View, which also moves it to another underlaying Playlist.
        /// </summary>
        /// <returns>Returns false if the record could not be added to the target playlist.</returns>
        public bool MoveRecord(RecordView targetView)
        {
            return MoveRecords(GapIndex, 1, targetView, null);
        }

        /// <summary>
        /// Move a Record to another View, which also moves it to another underlaying Playlist.
        /// </summary>
        /// <param name="targetIndex">Leave null to add it into the open gap.</param>
        /// <returns>Returns false if the record could not be added to the target playlist.</returns>
        public bool MoveRecord(RecordPackage recordPackage, RecordView targetView, int? targetIndex = null)
        {
            int index = IndexOf(recordPackage);
            if (index < 0)
                return false;

            return MoveRecords(index, 1, targetView, targetIndex);
        }

        /// <summary>
        /// Move a set of Records to another View, which also moves them to another underlaying Playlist.
        /// </summary>
        /// <param name="targetIndex">Leave null to add it into the open gap.</param>
        /// <returns>Returns false if the records could not be added to the target playlist.</returns>
        public bool MoveRecords(int index, int count, RecordView targetView, int? targetIndex = null)
        {
            if (targetView == null)
                throw new Exception("The target RecordView is \"null\"");

            if (_playlist == null)
                throw new Exception("The current playlist is \"null\"");

            if (targetView._playlist == null)
                throw new Exception("The target playlist is \"null\"");

            if (targetView._playlist.BufferSizeLeft < count)
            {
                GD.Print("Failed to Move a RecordPackage because the target playlist does not have enough space.");
                return false;
            }

            List<RecordPackage> packagesToRemove = new(count);
            for (int i = 0; i < count; i++)
            {
                packagesToRemove.Add(packages[index + i]);
                packages[index + i] = null;
            }

            if (targetIndex.HasValue)
                targetIndex = Math.Clamp(targetIndex.Value, 0, RecordCount);
            else
            {
                if (targetView.GapIndex <= 0)
                {
                    targetIndex = 0;
                }
                else if (targetView.GapIndex >= targetView.RecordCount)
                {
                    //einfach hinten anfügen
                    targetIndex = targetView.RecordCount;
                }
                //Wenn man auf eine Package im View zeigt, erwartet man, dass sie davor gelegt wird, und nicht ersetzt (was sie dahinter legen würde).
                //Deshalb wird getestet, ob der aktuelle Slot gerade frei ist. Es wird der davor genommen, falls nicht.
                else if (targetView.GapIndex >= 0 && targetView.packages[targetView.GapIndex] == null)
                {
                    targetIndex = targetView.GapIndex;
                }
                else
                {
                    targetIndex = targetView.GapIndex + 1;
                }
            }

            ignoreSongsRemovedEvent = true;
            targetView.ignoreSongsAddedEvent = true;
            for (int i = 0; i < count; i++)
            {
                RecordPackage package = packagesToRemove[i];
                if (!_playlist.RemoveSong(package.song))
                    continue;

                packages[index + i] = null;

                if (targetIndex.Value == targetView.RecordCount)
                {
                    targetView._playlist.AddSong(package.song);
                    targetView.packages.Add(package);
                }
                else
                {
                    targetView._playlist.InsertSongAt(package.song, targetIndex.Value);
                    targetView.packages.Insert(targetIndex.Value, package);
                }
            }

            //nicht RemoveRange verwenden, da evtl. in die gleiche Liste schon etwas eingefügt wurde, was alles verschiebt
            //wenn man vorher alles herauslöscht, geht die Lücke verloren, die ja angibt
            packages.RemoveAll(x => x == null);
            ignoreSongsRemovedEvent = false;
            targetView.ignoreSongsAddedEvent = false;
            PlaylistChanged?.Invoke(new()
            {
                packages = packagesToRemove,
                changeToView = targetView,      //Remove, so move to target View
            });
            targetView.PlaylistChanged?.Invoke(new()
            {
                packages = packagesToRemove,
                changeToView = null,            //Add, so move to none
            });
            return true;
        }

        // Setup Settings
        private const float recordPackageWidth = 0.25f;     //als wie breit eine recordPackage behandelt wird
        private const float scrollAreaSize = 0.3f;          //wie groß der Bereich ist, in dem gescrollt werden kann (link und rechts, zw. 0 und 1)
        private const float flipThresholdOffset = -0.2f;    //um wie viel das Maus-spiel verschoben ist
        private const float flipThreshold = 1.7f;           //wie viel spiel die der Mauszeiger hat

        // User Settings
        public bool useAutoScroll = true;                   //ob, wenn die Maus an die Kanten des RecordViews kommt, automatisch gescrollt werden soll
        public float autoScrollSensitivity = 40f;           //wie schnell es auto-scrollt
        public float scrollSensitivity = 1f;                //wie schnell es mit der Maus scrollt


        public int GapIndex => Math.Clamp((int)(_centeredGapIndex + (RecordCount / 2)), -1, RecordCount);
        private float _centeredGapIndex;

        private Vector3 Bounds => ((BoxShape3D)recordViewBounds.Shape).Size;

        public IAnimationXFunction FlickThroughRotationXAnimation { get; set; } = new BinaryFlickThroughRotationXAnimationFunction();
        public IAnimationYFunction FlickThroughRotationYAnimation { get; set; } = new SubtleRotationYAnimationFunction();

        /// <summary>
        /// Eine Interface, um die Blätter-Animation zu bestimmen.
        /// </summary>
        /// Es wurde ein Interface einem Delegaten vorgezogen, damit die Animationen evtl. eigene Einstellungen speichern können, oder können interne Daten pro gerenderten Frame anpassen, z.B. für Physik-Modelle.
        /// Ein Interface ist wesentlich erweiterbarer.
        public interface IAnimationXFunction
        {
            /// <summary>
            /// Wie nah darf die Gap an den Rand des RecordViews kommen? Um zu vermeiden, dass das Aktuell offene RecordPackage nur halb zu sehen ist.
            /// Da das vom Winkel des Packages abhängt muss es hier definiert sein.
            /// Zur positiven Seite hin.
            /// </summary>
            public float ForwardGapToViewBoundryMargin { get; }

            /// <summary>
            /// Wie nah darf die Gap an den Rand des RecordViews kommen? Um zu vermeiden, dass das Aktuell offene RecordPackage nur halb zu sehen ist.
            /// Da das vom Winkel des Packages abhängt muss es hier definiert sein.
            /// Zur negativen Seite hin.
            /// </summary>
            public float BackwardGapToViewBoundryMargin { get; }

            /// <summary>
            /// Eine Funktion, die den Winkel um die X-Achse für jede RecordPackage beschreibt, abhängig vom Abstand des Objektes zur Mausposition.
            /// </summary>
            public float AnimationFunction(Vector2 relativeMousePos);
        }

        public interface IAnimationYFunction
        {
            /// <summary>
            /// Eine Funktion, die den Winkel um die Y-Achse für jede RecordPackage beschreibt, abhängig vom Abstand des Objektes zur Mausposition.
            /// </summary>
            public float AnimationFunction(Vector2 relativeMousePos);
        }

        public struct BinaryFlickThroughRotationXAnimationFunction : IAnimationXFunction
        {
            public readonly float ForwardGapToViewBoundryMargin => 0.3f;
            public readonly float BackwardGapToViewBoundryMargin => 0.9f;

            public readonly float AnimationFunction(Vector2 relativeMousePos)
            {
                float maxXAngle = Mathf.DegToRad(50);

                return relativeMousePos.Y < 0 ? maxXAngle * 0.4f : -maxXAngle;
            }
        }

        public struct LeaningFlickThroughRotationXAnimationFunction : IAnimationXFunction
        {
            public readonly float ForwardGapToViewBoundryMargin => 1.1f;    //outdated
            public readonly float BackwardGapToViewBoundryMargin => 1.1f;   //outdated

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

        public struct SubtleRotationYAnimationFunction : IAnimationYFunction
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
            Playlist = playlist;
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

            //raycast hit nützt uns nur, wenn wir die Oberseite getroffen haben
            const float allowedInaccuracy = 0.05f;

            if (localPos.Y > Bounds.Y * (0.5f - allowedInaccuracy))
                return new Vector2(localPos.X, localPos.Z);
            else
                return null;
        }

        private void Scroll(float gaps)
        {
            float newPos = RecordsContainer.Position.Z - (gaps * recordPackageWidth);

            //so viel muss mindestens in beide richtungen gescrollt werden können, sonst erreicht man nicht alles
            float minimumScrollStop = RecordCount * 0.5f * recordPackageWidth - Bounds.Z * 0.5f;

            //Es wird bis hierher erlaubt zu scrollen: Es wird zB. 30% (0.3f) der Bounds.Z-Länge frei sein, wenn das Ende erreicht ist.
            const float relativeScrollStopOffset = 0.3f;

            float additionalScrollLength = Bounds.Z * relativeScrollStopOffset;

            float scrollStopLength = minimumScrollStop + additionalScrollLength;

            //die margins der Gap noch aufrechen, da man sonst evtl. Packages am Rand nicht mehr erreicht
            float scrollMax = scrollStopLength + FlickThroughRotationXAnimation.ForwardGapToViewBoundryMargin;
            float scrollMin = -scrollStopLength - FlickThroughRotationXAnimation.BackwardGapToViewBoundryMargin;

            newPos = Mathf.Clamp(newPos, scrollMin, scrollMax);
            RecordsContainer.Position = new Vector3(RecordsContainer.Position.X, RecordsContainer.Position.Y, newPos);
        }

        private void OnScrollInput(float lines)
        {
            Scroll(lines * scrollSensitivity);
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
            }
        }

        /// <summary>
        /// Wird aufgerufen vom GrabHandler, so wird ein Package herausgezogen. Es wird nur bewegt, nicht entfernt!
        /// </summary>
        public RecordPackage Grab()
        {
            var package = packages[Math.Clamp(GapIndex, 0, RecordCount - 1)];
            return package;
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
                float flipAreaMax = Bounds.Z *  0.5f - FlickThroughRotationXAnimation.ForwardGapToViewBoundryMargin;
                float flipAreaMin = Bounds.Z * -0.5f + FlickThroughRotationXAnimation.BackwardGapToViewBoundryMargin;
                float clamped = Mathf.Clamp(boundaryMousePos.Value.Y, flipAreaMin, flipAreaMax);
                Vector3 localPos = transform * new Vector3(boundaryMousePos.Value.X, 0, clamped);
                containerMousePos = new(localPos.X, localPos.Z);
            }
            else
            {
                Vector3 localPos = transform * new Vector3(boundaryMousePos.Value.X, 0, boundaryMousePos.Value.Y);
                containerMousePos = new(localPos.X, localPos.Z);
            }

            float mouseZDelta = containerMousePos.Y - lastMouseY;
            currentFlipOffset = Mathf.Clamp(currentFlipOffset + mouseZDelta, Mathf.Min(-flipThreshold * 0.5f + flipThresholdOffset, -currentFlipOffset), Mathf.Max(flipThreshold * 0.5f + flipThresholdOffset, currentFlipOffset));
            lastMouseY = containerMousePos.Y;
            _centeredGapIndex = (containerMousePos.Y - currentFlipOffset) / recordPackageWidth;

            for (int i = 0; i < _playlist.SongCount; i++)
            {
                var package = packages[i];
                Vector2 packageToMouse = new(containerMousePos.X - package.Position.X, _centeredGapIndex - (package.ViewIndex - RecordCount / 2));
                UpdatePackageTransforms(package, packageToMouse);
            }

            CutoffMaterialInstance.SetShaderParameter("box_transform", recordViewBounds.GlobalTransform);
            CutoffMaterialInstance.SetShaderParameter("box_size", ((BoxShape3D)recordViewBounds.Shape).Size);
        }

        private void UpdatePackageTransforms(RecordPackage package, Vector2 packageToMouse)
        {
            if (package.IsGettingDragged)
                return;

            package.Position = new(0, 0, (package.ViewIndex - (RecordCount / 2)) * recordPackageWidth);

            float xRotation = FlickThroughRotationXAnimation.AnimationFunction(packageToMouse);
            float yRotation = FlickThroughRotationYAnimation.AnimationFunction(packageToMouse);

            package.Rotation = new Vector3(xRotation, yRotation, 0);
        }
    }
}