using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musikspieler.Scripts.RecordView
{
	public abstract partial class ScrollView<T> : View where T : IItem
	{
		[Export] protected Node3D _scrollContainer;
		[Export] private CollisionShape3D viewBounds;
		public override CollisionShape3D BoundsShape => viewBounds;
		public override ScrollViewContentContainer Container => (ScrollViewContentContainer)_scrollContainer;

		protected readonly List<ViewItemGeneric<T>> itemObjects = [];

		public int ItemCount => itemObjects.Count;

		private ShaderMaterial _localMaterial;
		public override ShaderMaterial LocalMaterial => _localMaterial;

		public ViewItemGeneric<T> this[int index]
		{
			get { return itemObjects[index]; }
		}

		public override bool IsInitialized => ItemList != null;

		//changes that were made to the list of this view. Also fires when the list object gets changed, and automatically adapts to the new ItemLists events.
		public event Action<ItemsAddedEventArgs> ItemsAdded;
		public event Action<ItemsRemovedEventArgs> ItemsRemoved;

		public override event Action<ItemListChangedEventArgs> ObjectsChanged;

		private IItemList<T> _itemList;
		public IItemList<T> ItemList
		{
			get => _itemList;
			set
			{
				GD.Print("ScrollView: Setter ItemList");
				if (_itemList != null)
				{
					_itemList.ItemsAdded -= ItemsAdded;
					_itemList.ItemsRemoved -= ItemsRemoved;
					_itemList.ItemsAdded -= OnItemsAdded;
					_itemList.ItemsRemoved -= OnItemsRemoved;
					for (int i = 0; i < ItemCount; i++)
					{
						itemObjects[i].QueueFree();
					}
					GD.Print("why are we here just to suffer");
					itemObjects.Clear();
					ItemsRemoved?.Invoke(new()
					{
						count = _itemList.ItemCount,
						startIndex = 0,
					});
				}
				_itemList = value;
				if (_itemList != null)
				{
					List<ViewItemGeneric<T>> newItems = new(_itemList.ItemCount);
					for (int i = 0; i < _itemList.ItemCount; i++)
					{
						var item = ViewItemGeneric<T>.InstantiateAndAssign(this, i);
						newItems.Add(item);
						_scrollContainer.AddChild(item);
					}
					itemObjects.AddRange(newItems);
					ObjectsChanged?.Invoke(new()
					{
						itemsToChangeView = newItems.Cast<ViewItem>().ToList(),
						changeToView = this,
					});

					UpdateAllItemTransforms();
					_itemList.ItemsAdded += ItemsAdded;
					_itemList.ItemsRemoved += ItemsRemoved;
					_itemList.ItemsAdded += OnItemsAdded;
					_itemList.ItemsRemoved += OnItemsRemoved;
					ItemsAdded?.Invoke(new()
					{
						count = _itemList.ItemCount,
						startIndex = 0,
					});
				}
			}
		}

		// Setup Settings - Sollten im Konstruktor des abgeleiteten Objekts gesetzt werden!
		protected float itemObjectWidth = 0.25f;            //als wie breit eine displayedItem behandelt wird
		protected float scrollAreaSize = 0.3f;              //wie groß der Bereich ist, in dem gescrollt werden kann (link und rechts, zw. 0 und 1)
		protected float flipThresholdOffset = -0.2f;        //um wie viel das Maus-spiel verschoben ist
		protected float flipThreshold = 1.7f;               //wie viel spiel die der Mauszeiger hat

		// User Settings - TODO: an eine statsichen ort bringen, bzw. als globales setting machen
		public bool useAutoScroll = true;                   //ob, wenn die Maus an die Kanten des RecordViews kommt, automatisch gescrollt werden soll
		public float autoScrollSensitivity = 40f;           //wie schnell es auto-scrollt
		public float scrollSensitivity = 1f;                //wie schnell es mit der Maus scrollt

		//clamped to allow -1 and ItemCount, so that insertions can work correctly.
		public int GapIndex => Math.Clamp((int)(_centeredGapIndex + (ItemCount / 2)), -1, ItemCount);

		//clamped to be usable as an indexer.
		protected int GapIndexClamped => Math.Clamp((int)(_centeredGapIndex + (ItemCount / 2)), 0, ItemCount - 1);
		public T ItemAtGapIndex => ItemCount == 0 ? default :_itemList[GapIndexClamped];
		public ViewItemGeneric<T> ObjectAtGapIndex => ItemCount == 0 ? null : itemObjects[GapIndexClamped];
		private float _centeredGapIndex;

		public Vector3 Bounds => ((BoxShape3D)viewBounds.Shape).Size;

		public Animations Animation { get; set; }

		private bool ignoreItemsAddedEvent = false;
		private bool ignoreItemsRemovedEvent = false;

		public override void _Ready()
		{
			_localMaterial = (ShaderMaterial)ViewItemGeneric<T>.DefaultMaterial.Duplicate();
			base._Ready();
		}

		private void OnItemsAdded(ItemsAddedEventArgs args)
		{
			GD.Print("ScrollView: OnItemsAdded");
			if (ignoreItemsAddedEvent)
				return;

			List<ViewItemGeneric<T>> newItems = new(args.count);
			for (int i = 0; i < args.count; i++)
			{
				ViewItemGeneric<T> item = ViewItemGeneric<T>.InstantiateAndAssign(this, i);
				newItems.Add(item);
				Container.AddChild(item);
			}
			if (args.startIndex >= ItemCount)
				itemObjects.AddRange(newItems);
			else
				itemObjects.InsertRange(args.startIndex, newItems);
			ObjectsChanged?.Invoke(new()
			{
				itemsToChangeView = newItems.Cast<ViewItem>().ToList(),
				changeToView = null,
			});
			//UpdateAllItemTransforms();
		}

		private void OnItemsRemoved(ItemsRemovedEventArgs args)
		{
			GD.Print("ScrollView: OnItemsRemoved");
			if (ignoreItemsRemovedEvent)
				return;

			List<ViewItemGeneric<T>> itemsToDelete = new(args.count);
			for (int i = 0; i < args.count; i++)
			{
				itemsToDelete.Add(itemObjects[args.startIndex + i]);
				//displayedItem.QueueFree(); //macht jetzt der garbage bin
			}
			itemObjects.RemoveRange(args.startIndex, args.count);
			ObjectsChanged?.Invoke(new()
			{
				itemsToChangeView = itemsToDelete.Cast<ViewItem>().ToList(),
				//changeToView = GarbageBin<T>.Instance,
			});
			//UpdateAllItemTransforms();
		}

		public override int GetViewIndex(ViewItem item)
		{
			if (item is ViewItemGeneric<T> genericItem)
				return itemObjects.IndexOf(genericItem);
			return -1;
		}

		public override bool MoveItem(int index, View targetView)
        {
            GD.Print("as.kdjalskdfhakds");
            return MoveItem(index, targetView, null);
		}

		/// <summary>
		/// Move the open Record to another ChildView, which also moves it to another underlaying ItemList.
		/// </summary>
		/// <returns>Returns false if the record could not be added to the target playlist.</returns>
		public bool MoveItem(ScrollView<T> targetView)
		{
			GD.Print("as.kdjalskdfhakds");
			return MoveItem(GapIndex, targetView, null);
		}

		public bool MoveItem(int index, View targetView, int? targetIndex = null)
		{
			GD.Print("ScrollView: MoveItem");
			ArgumentNullException.ThrowIfNull(targetView, nameof(targetView));

			if (!IsInitialized)
			{
				GD.PrintErr("ScrollView has not been initialized.");
				return false;
			}

			if (ItemCount <= 0)
			{
				GD.Print("Nothing to remove");
				return false;
			}

			index = Math.Clamp(index, 0, ItemCount - 1);
			var itemToRemove = itemObjects[index];
			itemObjects[index] = null;

			ignoreItemsRemovedEvent = true;

			if (!targetView.AcceptItem(itemToRemove, targetIndex))
			{
				GD.Print("ScrollView: targetView did not AcceptItem");
				ignoreItemsRemovedEvent = false;
				itemObjects[index] = itemToRemove;
				return false;
			}
			GD.Print("ScrollView: targetView accepted Item, removing Item from this");
			_itemList.RemoveItem(itemToRemove.displayedItem);
			itemObjects.Remove(null);
			ObjectsChanged?.Invoke(new()
			{
				itemsToChangeView = [itemToRemove],
				changeToView = targetView,
			});
			ignoreItemsRemovedEvent = false;
			return true;
		}

		public override bool AcceptItem(ViewItem item, int? index)
		{
			GD.Print("ScrollView: AcceptItem");
			if (item is not ViewItemGeneric<T> viewItem)
			{
				GD.Print("ScrollView: Item type does not match the target views item type. Aborting.");
				return false;
			}

			if (index.HasValue)
				index = Math.Clamp(index.Value, 0, ItemCount);
			else
			{
				if (GapIndex <= 0)
				{
					index = 0;
				}
				else if (GapIndex >= ItemCount)
				{
					//einfach hinten anfügen
					index = ItemCount;
				}
				//Wenn man auf eine Package im ChildView zeigt, erwartet man, dass sie davor gelegt wird, und nicht ersetzt (was sie dahinter legen würde).
				//Deshalb wird getestet, ob der aktuelle Slot gerade frei ist. Es wird der davor genommen, falls nicht.
				else if (GapIndex >= 0 && itemObjects[GapIndex] == null)
				{
					index = GapIndex;
				}
				else
				{
					index = GapIndex + 1;
				}
			}

			ignoreItemsAddedEvent = true;

			if (index.Value == ItemCount)
			{
				GD.Print("ScrollView: Add");
				_itemList.AddItem(viewItem.displayedItem);
				itemObjects.Add(viewItem);
			}
			else
			{
				GD.Print("ScrollView: Insert");
				_itemList.InsertItemAt(viewItem.displayedItem, index.Value);
				itemObjects.Insert(index.Value, viewItem);
			}

			ObjectsChanged?.Invoke(new()
			{
				itemsToChangeView = [],
				changeToView = null,
			});
			ignoreItemsAddedEvent = false;
			return true;
		}

		//containerMousePos auf der Boundary
		private Vector2? GetBoundaryMousePosition()
		{
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
			float newPos = Container.Position.Z - (gaps * itemObjectWidth);

			//so viel muss mindestens in beide richtungen gescrollt werden können, sonst erreicht man nicht alles
			float minimumScrollStop = ItemCount * 0.5f * itemObjectWidth - Bounds.Z * 0.5f;

			//Es wird bis hierher erlaubt zu scrollen: Es wird zB. 30% (0.3f) der BoundsShape.Z-Länge frei sein, wenn das Ende erreicht ist.
			const float relativeScrollStopOffset = 0.3f;

			float additionalScrollLength = Bounds.Z * relativeScrollStopOffset;

			float scrollStopLength = minimumScrollStop + additionalScrollLength;

			//die margins der Gap noch aufrechen, da man sonst evtl. Packages am Rand nicht mehr erreicht
			float scrollMax = scrollStopLength + Animation.ForwardGapToViewBoundryMargin;
			float scrollMin = -scrollStopLength - Animation.BackwardGapToViewBoundryMargin;

			if (scrollMin > scrollMax)
				//die playlist ist zu wenig gefüllt, scrollen ist aus
				newPos = 0;
			else
				newPos = Mathf.Clamp(newPos, scrollMin, scrollMax);
			Container.Position = new Vector3(Container.Position.X, Container.Position.Y, newPos);
		}

		private void OnScrollInput(float lines)
		{
			Scroll(lines * scrollSensitivity);
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
				{
					if (!IsUnderCursor)
						return;
					if (mouseEvent.Pressed)
						OnScrollInput(-1f);
					GetViewport().SetInputAsHandled();
				}
				else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
				{
                    if (!IsUnderCursor)
                        return;
					if (mouseEvent.Pressed)
						OnScrollInput(1f);
					GetViewport().SetInputAsHandled();
				}
			}
		}

		/// <summary>
		/// Wird aufgerufen vom GrabHandler, so wird ein Package herausgezogen. Es wird nur bewegt, nicht entfernt!
		/// </summary>
		public override ViewItem GrabItem(bool allowGrabChildren)
		{
			GD.Print("ScrollView: GrabItem");
			if (ItemCount == 0)
				return null;

			var item = ObjectAtGapIndex;

			if (allowGrabChildren && item is IItemAndView itemAndView && itemAndView.ChildView.IsUnderCursor)
			{
				GD.Print("ScrollView: Grab Children");
				return itemAndView.ChildView.GrabItem(true);
			}
			return item;
		}

		//save data between frames
		private Vector2 lastMousePos;
		private float currentFlipOffset;

		public override void _Process(double delta)
		{
			LocalMaterial.SetShaderParameter("box_transform", viewBounds.GlobalTransform);
			LocalMaterial.SetShaderParameter("box_size", ((BoxShape3D)viewBounds.Shape).Size);

			base._Process(delta);
			Vector2? boundaryMousePos = GetBoundaryMousePosition();

			if (boundaryMousePos == null)
				return;

			Transform3D transform = Container.GlobalTransform.AffineInverse() * viewBounds.GlobalTransform;
			Vector2 containerMousePos;
			if (useAutoScroll)
			{
				float normalizedBoundaryPos = boundaryMousePos.Value.Y / Bounds.Z;
				float scroll = Mathf.Max(Mathf.Abs(normalizedBoundaryPos) - (0.5f - scrollAreaSize), 0) * Mathf.Sign(normalizedBoundaryPos);
				Scroll(scroll * (float)delta * autoScrollSensitivity);
				float flipAreaMax = Bounds.Z * 0.5f - Animation.ForwardGapToViewBoundryMargin;
				float flipAreaMin = Bounds.Z * -0.5f + Animation.BackwardGapToViewBoundryMargin;
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

		public override void UpdateItemTransform(int index)
		{
			var item = itemObjects[index];

			if (item == null)
				return;

			if (item.IsGettingDragged)
				return;

			Vector2 itemToMouse = new(lastMousePos.X - item.Position.X, _centeredGapIndex - (item.ViewIndex - ItemCount / 2));

			float posZ = (item.ViewIndex - (ItemCount / 2)) * itemObjectWidth;

			AnimationInput animationInput = new()
			{
				PackagePos = posZ,
				relativeMousePos = itemToMouse,
				isSelected = index == GapIndex,
				index = index,
			};

			AnimationOutput output = Animation.RunAnimationFrame(animationInput);

			item.Position = output.PositionOffset + new Vector3(0, 0, posZ);
			item.Rotation = output.RotationOffset;
			item.Scale = output.ScaleOffset;
		}
	}
}
