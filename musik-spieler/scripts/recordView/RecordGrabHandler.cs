using Godot;
using System;

namespace Musikspieler.Scripts
{
    public partial class RecordGrabHandler : Node
    {
        private RecordPackage currentlyGrabbed;

        public static RecordGrabHandler Instance { get; private set; }

        //Hier werden alle durch externe Manipulation der Playlist gelöschten Records automatisch hinbewegt.
        public RecordView GarbageBin { get; private set; }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (currentlyGrabbed != null)
            {
                if (Utility.CameraRaycast(GetViewport().GetCamera3D(), CollisionMask.GlobalDragPlane, out var result))
                {
                    currentlyGrabbed.Position = (Vector3)result["position"];
                    GD.Print(currentlyGrabbed.Position);
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
        }

        private void OnLeftClick(bool isPressed)
        {
            //RayCast
            Mask<CollisionMask> mask = new(CollisionMask.RecordViewBoundary, CollisionMask.DrawerViewBoundary);
            if (Utility.CameraRaycast(GetViewport().GetCamera3D(), mask, out var result))
            {
                if (result != null && result.Count > 0 && (Node3D)result["collider"] is RecordView recordView)
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
                //Da die Platte ihre RecordView und ihren Index noch hat, muss nur das hier wieder gesetzt werden:
                currentlyGrabbed.IsGettingDragged = false;
                currentlyGrabbed = null;
            }
        }

        private void GrabRecord(RecordView recordView)
        {
            GD.Print("grab");
            currentlyGrabbed = recordView.Grab();
            currentlyGrabbed.IsGettingDragged = true;
        }

        private void PutRecord(RecordView recordView)
        {
            GD.Print("put");
            if (currentlyGrabbed == null)
                throw new Exception("Cannot put Record \"null\" into a RecordView.");

            currentlyGrabbed.RecordView.MoveRecord(currentlyGrabbed, recordView);
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
        }
    }
}
