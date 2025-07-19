using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Musikspieler.Scripts.RecordView
{
    public abstract partial class View : Node3D
    {
        public abstract ViewItem Grab();
        public abstract bool MoveRecord(int index, View targetView);
        public abstract bool IsItemListAssigned { get; }
    }

    public partial class GarbageBin : View
    {
        public static GarbageBin Instance { get; private set; }

        public override void _Ready()
        {
            base._Ready();
            if (Instance != null)
                throw new Exception("More than one GarbageBin exist.");
            Instance = this;
        }

        public override bool IsItemListAssigned => true;

        public override ViewItem Grab() => null;     //der Mülleimer gibt nie etwas her

        public override bool MoveRecord(int index, View targetView) => false;  //man kann nichts rausnehmen
    }

    public abstract partial class ScrollView<T> : View where T : IItem
    {
        [Export] protected Node3D _scrollContainer;
        [Export] protected CollisionShape3D viewBounds;
        public ScrollViewContentContainer ScrollContainer => (ScrollViewContentContainer)_scrollContainer;

        protected List<ViewItem<T>> items;

        public int ItemCount => items.Count;

        public ViewItem<T> this[int index]
        {
            get { return items[index]; }
        }

        public override bool IsItemListAssigned => Playlist != null;

        private IItemList<T> _playlist;
        public IItemList<T> Playlist
        {
            get => _playlist;
            set
            {
                if (_playlist != null)
                {
                    _playlist.ItemsAdded -= OnSongsAdded;
                    _playlist.ItemsRemoved -= OnSongsRemoved;
                    for (int i = 0; i < ItemCount; i++)
                    {
                        items[i].QueueFree();
                    }
                    items = null;
                }
                _playlist = value;
                if (_playlist != null)
                {
                    items = new(_playlist.ItemCount);
                    for (int i = 0; i < _playlist.ItemCount; i++)
                    {
                        items.Add(ViewItem<T>.InstantiateAndAssign(this, i));
                    }
                    _playlist.ItemsAdded += OnSongsAdded;
                    _playlist.ItemsRemoved += OnSongsRemoved;
                }
            }
        }

        private bool ignoreSongsAddedEvent = false;
        private bool ignoreSongsRemovedEvent = false;

        public event Action<PlaylistChangedEventArgs> PlaylistChanged = delegate { };

        public struct PlaylistChangedEventArgs
        {
            public readonly bool RecordsRemoved => changeToView != null;
            public readonly bool RecordsAdded => changeToView == null;

            public List<ViewItem<T>> packages;
            public ScrollView<T> changeToView;
        }

        private void OnSongsAdded(ItemsAddedEventArgs args)
        {
            if (ignoreSongsAddedEvent)
                return;

            List<ViewItem<T>> newPackages = new(args.count);
            for (int i = 0; i < args.count; i++)
            {
                ViewItem<T> package = ViewItem<T>.InstantiateAndAssign(this, i);
                newPackages.Add(package);
                ScrollContainer.AddChild(package);
            }
            if (args.startIndex >= ItemCount)
                items.AddRange(newPackages);
            else
                items.InsertRange(args.startIndex, newPackages);
            PlaylistChanged?.Invoke(new()
            {
                packages = newPackages,
                changeToView = null,
            });
            UpdateAllPackageTransforms();
        }

        private void OnSongsRemoved(ItemsRemovedEventArgs args)
        {
            if (ignoreSongsRemovedEvent)
                return;

            List<ViewItem<T>> packagesToDelete = new(args.count);
            for (int i = 0; i < args.count; i++)
            {
                packagesToDelete.Add(items[args.startIndex + i]);
                //package.QueueFree(); //macht jetzt der garbage bin
            }
            items.RemoveRange(args.startIndex, args.count);
            PlaylistChanged?.Invoke(new()
            {
                packages = packagesToDelete,
                //changeToView = GarbageBin<T>.Instance,
            });
            UpdateAllPackageTransforms();
        }

        public int IndexOf(ViewItem<T> recordPackage)
        {
            return items.IndexOf(recordPackage);
        }
        public ShaderMaterial CutoffMaterialInstance { get; private set; }

        public override bool MoveRecord(int index, View targetView)
        {
            if (targetView is ScrollView<T> scrollView)
            {
                return MoveRecords(index, 1, scrollView, null);
            }
            else
            {
                return MoveRecords(index, 1, targetView, null);
            }
        }

        /// <summary>
        /// Move the open Record to another View, which also moves it to another underlaying Playlist.
        /// </summary>
        /// <returns>Returns false if the record could not be added to the target playlist.</returns>
        public bool MoveRecord(ScrollView<T> targetView)
        {
            return MoveRecords(GapIndex, 1, targetView, null);
        }

        public bool MoveRecords(int index, int count, View targetView, int? targetIndex = null)
        {
            if (targetView is ScrollView<T> scrollView)
                return MoveRecords(index, count, scrollView, targetIndex);

            MoveChecks(index, count, targetView, targetIndex);

            //do stuff
            //TODO

            //hier landet man theoretisch nur, wenn man etwas in den Mülleimer schmeißt,
            //da es bisher nur dieses Objekt gibt, was tatsächlich mehrere Typen an Items annimmt.

            return true;
        }

        private void MoveChecks(int index, int count, View targetView, int? targetIndex = null)
        {
            ArgumentNullException.ThrowIfNull(targetView, nameof(targetView));

            if (_playlist == null)
                throw new NullReferenceException($"The ScrollView {nameof(_playlist)} is null.");

            if (!targetView.IsItemListAssigned)
                throw new NullReferenceException($"The targetView has no ItemList assigned.");

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count, nameof(count));

            ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));

            if (targetIndex.HasValue)
                ArgumentOutOfRangeException.ThrowIfNegative(targetIndex.Value, nameof(targetIndex));
        }

        /// <summary>
        /// Move a set of Records to another View, which also moves them to another underlaying Playlist.
        /// </summary>
        /// <param name="targetIndex">Leave null to add it into the open gap.</param>
        /// <returns>Returns false if the records could not be added to the target playlist.</returns>
        public bool MoveRecords(int index, int count, ScrollView<T> targetView, int? targetIndex = null)
        {
            MoveChecks(index, count, targetView, targetIndex);

            if (targetView._playlist.BufferSizeLeft < count)
            {
                GD.Print("Failed to Move a RecordPackage because the target playlist does not have enough space.");
                return false;
            }

            List<ViewItem<T>> packagesToRemove = new(count);
            for (int i = 0; i < count; i++)
            {
                packagesToRemove.Add(items[index + i]);
                items[index + i] = null;
            }

            if (targetIndex.HasValue)
                targetIndex = Math.Clamp(targetIndex.Value, 0, ItemCount);
            else
            {
                if (targetView.GapIndex <= 0)
                {
                    targetIndex = 0;
                }
                else if (targetView.GapIndex >= targetView.ItemCount)
                {
                    //einfach hinten anfügen
                    targetIndex = targetView.ItemCount;
                }
                //Wenn man auf eine Package im View zeigt, erwartet man, dass sie davor gelegt wird, und nicht ersetzt (was sie dahinter legen würde).
                //Deshalb wird getestet, ob der aktuelle Slot gerade frei ist. Es wird der davor genommen, falls nicht.
                else if (targetView.GapIndex >= 0 && targetView.items[targetView.GapIndex] == null)
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
                ViewItem<T> package = packagesToRemove[i];
                if (!_playlist.RemoveItem(package.song))
                    continue;

                items[index + i] = null;

                if (targetIndex.Value == targetView.ItemCount)
                {
                    targetView._playlist.AddItem(package.song);
                    targetView.items.Add(package);
                }
                else
                {
                    targetView._playlist.InsertItemAt(package.song, targetIndex.Value);
                    targetView.items.Insert(targetIndex.Value, package);
                }
            }

            //nicht RemoveRange verwenden, da evtl. in die gleiche Liste schon etwas eingefügt wurde, was alles verschiebt
            //wenn man vorher alles herauslöscht, geht die Lücke verloren, die ja angibt
            items.RemoveAll(x => x == null);
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
            UpdateAllPackageTransforms();
            targetView.UpdateAllPackageTransforms();
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


        public int GapIndex => Math.Clamp((int)(_centeredGapIndex + (ItemCount / 2)), -1, ItemCount);
        private float _centeredGapIndex;

        private Vector3 Bounds => ((BoxShape3D)viewBounds.Shape).Size;

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
            CutoffMaterialInstance = (ShaderMaterial)ViewItem<T>.DefaultMaterial.Duplicate();
            base._Ready();
        }

        //containerMousePos auf der Boundary
        private Vector2? GetBoundaryMousePosition()
        {
            Mask<CollisionMask> mask = CollisionMask.RecordViewBoundary;

            //getroffen?
            if (!Utility.CameraRaycast(GetViewport().GetCamera3D(), mask, out var result))
                return null;

            //unseres getroffen?
            if ((Node)result["collider"] != viewBounds.GetParent())
                return null;

            Vector3 hitPos = (Vector3)result["position"];
            Vector3 localPos = viewBounds.GlobalTransform.AffineInverse() * hitPos;

            //raycast hit nützt uns nur, wenn wir die Oberseite getroffen haben
            const float allowedInaccuracy = 0.05f;

            if (localPos.Y > Bounds.Y * (0.5f - allowedInaccuracy))
                return new Vector2(localPos.X, localPos.Z);
            else
                return null;
        }

        private void Scroll(float gaps)
        {
            float newPos = ScrollContainer.Position.Z - (gaps * recordPackageWidth);

            //so viel muss mindestens in beide richtungen gescrollt werden können, sonst erreicht man nicht alles
            float minimumScrollStop = ItemCount * 0.5f * recordPackageWidth - Bounds.Z * 0.5f;

            //Es wird bis hierher erlaubt zu scrollen: Es wird zB. 30% (0.3f) der Bounds.Z-Länge frei sein, wenn das Ende erreicht ist.
            const float relativeScrollStopOffset = 0.3f;

            float additionalScrollLength = Bounds.Z * relativeScrollStopOffset;

            float scrollStopLength = minimumScrollStop + additionalScrollLength;

            //die margins der Gap noch aufrechen, da man sonst evtl. Packages am Rand nicht mehr erreicht
            float scrollMax = scrollStopLength + FlickThroughRotationXAnimation.ForwardGapToViewBoundryMargin;
            float scrollMin = -scrollStopLength - FlickThroughRotationXAnimation.BackwardGapToViewBoundryMargin;

            if (scrollMin > scrollMax)
                //die playlist ist zu wenig gefüllt, scrollen ist aus
                newPos = 0;
            else
                newPos = Mathf.Clamp(newPos, scrollMin, scrollMax);
            ScrollContainer.Position = new Vector3(ScrollContainer.Position.X, ScrollContainer.Position.Y, newPos);
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
        public override ViewItem Grab()
        {
            if (ItemCount == 0)
                return null;

            var package = items[Math.Clamp(GapIndex, 0, ItemCount - 1)];
            return package;
        }

        private Vector2 lastMousePos;
        private float currentFlipOffset;

        public override void _Process(double delta)
        {
            CutoffMaterialInstance.SetShaderParameter("box_transform", viewBounds.GlobalTransform);
            CutoffMaterialInstance.SetShaderParameter("box_size", ((BoxShape3D)viewBounds.Shape).Size);

            base._Process(delta);
            Vector2? boundaryMousePos = GetBoundaryMousePosition();

            if (boundaryMousePos == null)
                return;

            Transform3D transform = ScrollContainer.GlobalTransform.AffineInverse() * viewBounds.GlobalTransform;
            Vector2 containerMousePos;
            if (useAutoScroll)
            {
                float normalizedBoundaryPos = boundaryMousePos.Value.Y / Bounds.Z;
                float scroll = Mathf.Max(Mathf.Abs(normalizedBoundaryPos) - (0.5f - scrollAreaSize), 0) * Mathf.Sign(normalizedBoundaryPos);
                Scroll(scroll * (float)delta * autoScrollSensitivity);
                float flipAreaMax = Bounds.Z * 0.5f - FlickThroughRotationXAnimation.ForwardGapToViewBoundryMargin;
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

            float mouseZDelta = containerMousePos.Y - lastMousePos.Y;
            currentFlipOffset = Mathf.Clamp(currentFlipOffset + mouseZDelta, Mathf.Min(-flipThreshold * 0.5f + flipThresholdOffset, -currentFlipOffset), Mathf.Max(flipThreshold * 0.5f + flipThresholdOffset, currentFlipOffset));
            lastMousePos = containerMousePos;
            _centeredGapIndex = (containerMousePos.Y - currentFlipOffset) / recordPackageWidth;

            UpdateAllPackageTransforms();
        }

        public void UpdateAllPackageTransforms()
        {
            for (int i = 0; i < _playlist.ItemCount; i++)
            {
                UpdatePackageTransform(i);
            }
        }

        public void UpdatePackageTransform(int index)
        {
            var package = items[index];

            if (package.IsGettingDragged)
                return;

            Vector2 packageToMouse = new(lastMousePos.X - package.Position.X, _centeredGapIndex - (package.ViewIndex - ItemCount / 2));

            package.Position = new(0, 0, (package.ViewIndex - (ItemCount / 2)) * recordPackageWidth);

            float xRotation = FlickThroughRotationXAnimation.AnimationFunction(packageToMouse);
            float yRotation = FlickThroughRotationYAnimation.AnimationFunction(packageToMouse);

            package.Rotation = new Vector3(xRotation, yRotation, 0);
        }

    }

    public partial class RecordView : ScrollView<ISong>
    {
        public override void _Ready()
        {
            base._Ready();

            //NUR FÜR TESTZWECKE
            GD.Print("RecordView created");
            List<ISong> songs = new(100);
            for (int i = 0; i < 100; i++)
            {
                songs.Add(new Song(Utility.RandomString(10)));
            }
            Playlist playlist = new("Playlist");
            Playlist = playlist;
            playlist.AddItems(songs);
        }
    }
}