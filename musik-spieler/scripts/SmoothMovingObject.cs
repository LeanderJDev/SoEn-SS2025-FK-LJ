using Godot;
using System;

namespace Musikspieler.Scripts
{
    public partial class SmoothMovingObject : Node3D
    {
        //ausblenden, damit andere Klassen nicht wissen müssen, das das hier ein bewegungs-modifiziertes Objekt ist
        public new Vector3 Position
        {
            get => MovementState.targetPosition;
            set => MovementState.targetPosition = value;
        }

        public new Vector3 Rotation
        {
            get => MovementState.targetRotation;
            set => MovementState.targetRotation = value;
        }

        public new Vector3 Scale
        {
            get => MovementState.targetScale;
            set => MovementState.targetScale = value;
        }

        private SmoothDamp.SmoothMovementState _movementState;
        protected SmoothDamp.SmoothMovementState MovementState
        {
            get => _movementState;
            set
            {
                if (value != null)
                    _movementState = value;
            }
        }

        protected SmoothDamp SmoothDamp { get; set; }

        public override void _Process(double delta)
        {
            base._Process(delta);

            SmoothDamp.Step(this, MovementState, (float)delta);
        }

        public void Teleport(Vector3 pos, Vector3 rot)
        {
            Position = pos;
            Rotation = rot;
            base.Position = pos;
            base.Rotation = rot;
        }

        public SmoothMovingObject()
        {
            _movementState = new(this);
        }
    }
}
