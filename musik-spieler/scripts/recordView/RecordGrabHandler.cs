using Godot;
using System;
using System.Collections.Generic;

namespace Musikspieler.Scripts.RecordView
{
    public partial class RecordGrabHandler : Node
    {
        private ViewItem currentlyGrabbed;

        public static RecordGrabHandler Instance { get; private set; }

        //Hier werden alle durch externe Manipulation der ItemList gelöschten Records automatisch hinbewegt.
        public RecordView GarbageBin { get; private set; }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (currentlyGrabbed != null)
            {
                if (Utility.CameraRaycast(GetViewport().GetCamera3D(), CollisionMask.GlobalDragPlane, out var result))
                {
                    currentlyGrabbed.Position = (Vector3)result["position"];
                }
            }
        }

        private RecordGrabHandler()
        {
            if (Instance != null)
                throw new Exception("There seem to be more than one GrabHandler in the Scene.");
            Instance = this;
        }

        public override void _Ready()
        {
            base._Ready();

            //TODO: garbage bin hier initialisieren/Referenz bekommen
            // Dafür würde man an sich eine [Export] public Node3D Property benutzen
        }

        private void OnLeftClick(bool isPressed)
        {
            //RayCast
            Mask<CollisionMask> mask = new(CollisionMask.RecordViewBoundary, CollisionMask.DrawerViewBoundary);

            List<StaticBody3D> exludedObjects = null;
            if (currentlyGrabbed is IItemAndView view)
            {
                //cast geht, da Godot vorschreibt, dass ein CollisionShape3D-Objekt eine StaticBody3D als Parent haben muss.
                exludedObjects = [view.ChildView];
            }

            if (Utility.CameraRaycast(GetViewport().GetCamera3D(), mask, out var result, exludedObjects))
            {
                if (result != null && result.Count > 0 && (Node3D)result["collider"] is View recordView)
                {
                    if (isPressed && currentlyGrabbed == null)
                    {
                        //Es wird versucht, etwas herauszunehmen, an einer validen Stelle.
                        GrabRecord(recordView);
                    }
                    else if (!isPressed && currentlyGrabbed != null)
                    {
                        //Es wird versucht, etwas anzulegen, an einer validen Stelle.
                        PutRecord(recordView);
                    }
                }
            }
            else
            {
                if (isPressed)
                    //Es wird versucht, etwas zu ziehen wo nichts ist. Es passiert nichts.
                    return;

                if (currentlyGrabbed == null)
                    //Es wird versucht, "Nichts" abzulegen. Das geht natürlich nicht.
                    return;

                //Es wird versucht etwas abzulegen, wo nichts ist. Die Platte muss zurück, wo sie herkommt.
                //Da die Platte ihre ChildView und ihren Index noch hat, muss nur das hier wieder gesetzt werden:
                currentlyGrabbed.IsGettingDragged = false;
                currentlyGrabbed = null;
            }
        }

        private void GrabRecord(View recordView)
        {
            GD.Print("GrabHandler: Grab");
            currentlyGrabbed = recordView.GrabItem(true);
            if (currentlyGrabbed == null)
                return;

            currentlyGrabbed.IsGettingDragged = true;
        }

        private void PutRecord(View recordView)
        {
            GD.Print("GrabHandler: Put");
            if (currentlyGrabbed == null)
                throw new Exception("Cannot put Record \"null\" into a RecordView.");

            if (recordView == null)
                throw new Exception();

            currentlyGrabbed.Move(recordView);
            currentlyGrabbed.IsGettingDragged = false;

            currentlyGrabbed = null;
        }

        private void OnRightClick(bool isPressed)
        {
            //aborts the dragging TODO - wird wahrscheinlich andere Funktion bekommen
            if (currentlyGrabbed != null)
            {
                currentlyGrabbed.IsGettingDragged = false;
                currentlyGrabbed = null;
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    OnLeftClick(mouseEvent.Pressed);
                }
                else if (mouseEvent.ButtonIndex == MouseButton.Right)
                {
                    OnRightClick(mouseEvent.Pressed);
                }
            }
            base._Input(@event);
        }
    }
}
