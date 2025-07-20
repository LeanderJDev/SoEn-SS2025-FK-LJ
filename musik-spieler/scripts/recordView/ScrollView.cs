using Godot;
using System;
using System.Collections.Generic;

namespace Musikspieler.Scripts.RecordView
{
    public abstract partial class ScrollView<T> : View where T : IItem
    {
        [Export] protected Node3D _scrollContainer;
        [Export] protected CollisionShape3D viewBounds;
        public ScrollViewContentContainer ScrollContainer => (ScrollViewContentContainer)_scrollContainer;

        protected readonly List<ViewItemGeneric<T>> itemObjects = [];

        public int ItemCount => itemObjects.Count;

        public ViewItemGeneric<T> this[int index]
        {
            get { return itemObjects[index]; }
        }

        public override bool IsItemListAssigned => ItemList != null;

        private IItemList<T> _itemList;
        public IItemList<T> ItemList
        {
            get => _itemList;
            set
            {
                if (_itemList != null)
                {
                    _itemList.ItemsAdded -= OnItemsAdded;
                    _itemList.ItemsRemoved -= OnItemsRemoved;
                    for (int i = 0; i < ItemCount; i++)
                    {
                        itemObjects[i].QueueFree();
                    }
                    itemObjects.Clear();
                }
                _itemList = value;
                if (_itemList != null)
                {
                    for (int i = 0; i < _itemList.ItemCount; i++)
                    {
                        itemObjects.Add(ViewItemGeneric<T>.InstantiateAndAssign(this, i));
                    }
                    _itemList.ItemsAdded += OnItemsAdded;
                    _itemList.ItemsRemoved += OnItemsRemoved;
                }
            }
        }

        // Setup Settings
        private const float itemObjectWidth = 0.25f;        //als wie breit eine item behandelt wird
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

        private bool ignoreItemsAddedEvent = false;
        private bool ignoreItemsRemovedEvent = false;

        public event Action<PlaylistChangedEventArgs> ItemListChanged = delegate { };

        public struct PlaylistChangedEventArgs
        {
            public readonly bool ItemsRemoved => changeToView != null;
            public readonly bool ItemsAdded => changeToView == null;

            public List<ViewItemGeneric<T>> items;
            public ScrollView<T> changeToView;
        }

        public override void _Ready()
        {
            CutoffMaterialInstance = (ShaderMaterial)ViewItemGeneric<T>.DefaultMaterial.Duplicate();
            base._Ready();
        }

        private void OnItemsAdded(ItemsAddedEventArgs args)
        {
            if (ignoreItemsAddedEvent)
                return;

            List<ViewItemGeneric<T>> newItems = new(args.count);
            for (int i = 0; i < args.count; i++)
            {
                ViewItemGeneric<T> item = ViewItemGeneric<T>.InstantiateAndAssign(this, i);
                newItems.Add(item);
                ScrollContainer.AddChild(item);
            }
            if (args.startIndex >= ItemCount)
                itemObjects.AddRange(newItems);
            else
                itemObjects.InsertRange(args.startIndex, newItems);
            ItemListChanged?.Invoke(new()
            {
                items = newItems,
                changeToView = null,
            });
            UpdateAllItemTransforms();
        }

        private void OnItemsRemoved(ItemsRemovedEventArgs args)
        {
            if (ignoreItemsRemovedEvent)
                return;

            List<ViewItemGeneric<T>> itemsToDelete = new(args.count);
            for (int i = 0; i < args.count; i++)
            {
                itemsToDelete.Add(itemObjects[args.startIndex + i]);
                //item.QueueFree(); //macht jetzt der garbage bin
            }
            itemObjects.RemoveRange(args.startIndex, args.count);
            ItemListChanged?.Invoke(new()
            {
                items = itemsToDelete,
                //changeToView = GarbageBin<T>.Instance,
            });
            UpdateAllItemTransforms();
        }

        public int IndexOf(ViewItemGeneric<T> item)
        {
            return itemObjects.IndexOf(item);
        }
        public ShaderMaterial CutoffMaterialInstance { get; private set; }

        public override bool MoveItem(int index, View targetView)
        {
            if (targetView is ScrollView<T> scrollView)
            {
                return MoveItems(index, 1, scrollView, null);
            }
            else
            {
                return MoveItems(index, 1, targetView, null);
            }
        }

        /// <summary>
        /// Move the open Record to another View, which also moves it to another underlaying ItemList.
        /// </summary>
        /// <returns>Returns false if the record could not be added to the target playlist.</returns>
        public bool MoveItem(ScrollView<T> targetView)
        {
            return MoveItems(GapIndex, 1, targetView, null);
        }

        public bool MoveItems(int index, int count, View targetView, int? targetIndex = null)
        {
            if (targetView is ScrollView<T> scrollView)
                return MoveItems(index, count, scrollView, targetIndex);

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

            if (ItemList == null)
                throw new NullReferenceException($"The ScrollView {nameof(ItemList)} is null.");

            if (!targetView.IsItemListAssigned)
                throw new NullReferenceException($"The targetView has no ItemList assigned.");

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count, nameof(count));

            ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));

            if (targetIndex.HasValue)
                ArgumentOutOfRangeException.ThrowIfNegative(targetIndex.Value, nameof(targetIndex));
        }

        /// <summary>
        /// Move a set of Records to another View, which also moves them to another underlaying ItemList.
        /// </summary>
        /// <param name="targetIndex">Leave null to add it into the open gap.</param>
        /// <returns>Returns false if the records could not be added to the target playlist.</returns>
        public bool MoveItems(int index, int count, ScrollView<T> targetView, int? targetIndex = null)
        {
            MoveChecks(index, count, targetView, targetIndex);

            if (targetView.ItemList.BufferSizeLeft < count)
            {
                GD.Print("Failed to move an Item because the target itemlist does not have enough space.");
                return false;
            }

            List<ViewItemGeneric<T>> itemsToRemove = new(count);
            for (int i = 0; i < count; i++)
            {
                itemsToRemove.Add(itemObjects[index + i]);
                itemObjects[index + i] = null;
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
                else if (targetView.GapIndex >= 0 && targetView.itemObjects[targetView.GapIndex] == null)
                {
                    targetIndex = targetView.GapIndex;
                }
                else
                {
                    targetIndex = targetView.GapIndex + 1;
                }
            }

            ignoreItemsRemovedEvent = true;
            targetView.ignoreItemsAddedEvent = true;
            for (int i = 0; i < count; i++)
            {
                ViewItemGeneric<T> item = itemsToRemove[i];
                if (!_itemList.RemoveItem(item.song))
                    continue;

                itemObjects[index + i] = null;

                if (targetIndex.Value == targetView.ItemCount)
                {
                    targetView._itemList.AddItem(item.song);
                    targetView.itemObjects.Add(item);
                }
                else
                {
                    targetView._itemList.InsertItemAt(item.song, targetIndex.Value);
                    targetView.itemObjects.Insert(targetIndex.Value, item);
                }
            }

            //nicht RemoveRange verwenden, da evtl. in die gleiche Liste schon etwas eingefügt wurde, was alles verschiebt
            //wenn man vorher alles herauslöscht, geht die Lücke verloren, die ja angibt
            itemObjects.RemoveAll(x => x == null);
            ignoreItemsRemovedEvent = false;
            targetView.ignoreItemsAddedEvent = false;
            ItemListChanged?.Invoke(new()
            {
                items = itemsToRemove,
                changeToView = targetView,      //Remove, so move to target View
            });
            targetView.ItemListChanged?.Invoke(new()
            {
                items = itemsToRemove,
                changeToView = null,            //Add, so move to none
            });
            UpdateAllItemTransforms();
            targetView.UpdateAllItemTransforms();
            return true;
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
            float newPos = ScrollContainer.Position.Z - (gaps * itemObjectWidth);

            //so viel muss mindestens in beide richtungen gescrollt werden können, sonst erreicht man nicht alles
            float minimumScrollStop = ItemCount * 0.5f * itemObjectWidth - Bounds.Z * 0.5f;

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

            var package = itemObjects[Math.Clamp(GapIndex, 0, ItemCount - 1)];
            return package;
        }

        //save data between frames
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
            _centeredGapIndex = (containerMousePos.Y - currentFlipOffset) / itemObjectWidth;

            UpdateAllItemTransforms();
        }

        public void UpdateAllItemTransforms()
        {
            for (int i = 0; i < ItemCount; i++)
            {
                UpdateItemTransform(i);
            }
        }

        public void UpdateItemTransform(int index)
        {
            var item = itemObjects[index];

            if (item.IsGettingDragged)
                return;

            Vector2 itemToMouse = new(lastMousePos.X - item.Position.X, _centeredGapIndex - (item.ViewIndex - ItemCount / 2));

            item.Position = new(0, 0, (item.ViewIndex - (ItemCount / 2)) * itemObjectWidth);

            float xRotation = FlickThroughRotationXAnimation.AnimationFunction(itemToMouse);
            float yRotation = FlickThroughRotationYAnimation.AnimationFunction(itemToMouse);

            item.Rotation = new Vector3(xRotation, yRotation, 0);
        }
    }
}