using Godot;

namespace Musikspieler.Scripts.RecordView
{
    public partial class SmoothMovingObject : Node3D
    {
        //ausblenden, da wir quasi die eigentliche Position verdecken hier ersetzen, und einen zwischenlayer bauen
        public new Vector3 Position
        {
            get => SmoothDamp.PositionParameters is null ? base.Position : MovementState.targetPosition;
            set
            {
                if (SmoothDamp.PositionParameters is null)
                    base.Position = value;
                else
                    MovementState.targetPosition = value;
            }
        }

        public new Vector3 Rotation
        {
            get => SmoothDamp.RotationParameters is null ? base.Rotation : MovementState.targetRotation;
            set
            {
                if (SmoothDamp.RotationParameters is null)
                    base.Rotation = value;
                else
                    MovementState.targetRotation = value;
            }
        }

        public new Vector3 Scale
        {
            get => SmoothDamp.ScaleParameters is null ? base.Scale : MovementState.targetScale;
            set
            {
                if (SmoothDamp.ScaleParameters is null)
                    base.Scale = value;
                else
                    MovementState.targetScale = value;
            }
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

        public bool IsCloseToTargetPosition => (Position - base.Position).LengthSquared() < 0.1f;
        public bool IsCloseToTargetRotation => (Rotation - base.Rotation).LengthSquared() < 0.1f;
        public bool IsCloseToTargetScale => (Scale - base.Scale).LengthSquared() < 0.1f;

        public SmoothDamp SmoothDamp { get; protected set; }

        public override void _Process(double delta)
        {
            base._Process(delta);

            SmoothDamp.Step(this, MovementState, (float)delta);
        }

        /// <summary>
        /// Teleport to specific targets immediately.
        /// </summary>
        public void Teleport(Vector3? pos, Vector3? rot, Vector3? scale)
        {
            if (pos.HasValue)
            {
                Position = pos.Value;
                base.Position = pos.Value;
            }
            if (rot.HasValue)
            {
                Rotation = rot.Value;
                base.Rotation = rot.Value;
            }
            if (scale.HasValue)
            {
                Scale = scale.Value;
                base.Scale = scale.Value;
            }
        }

        /// <summary>
        /// Teleport to the respective targets immediately.
        /// </summary>
        public void Teleport()
        {
            base.Position = Position;
            base.Rotation = Rotation;
            base.Scale = Scale;
        }

        public void SmoothReparent(Node3D newParent)
        {
            // Alte Ziel-Transforms in globalen Raum bringen
            Vector3 globalTargetPos = GlobalTransform * MovementState.targetPosition;
            Vector3 globalTargetRot = GlobalTransform.Basis * MovementState.targetRotation;
            Vector3 globalTargetScale = GlobalTransform.Basis.Scale * MovementState.targetScale;

            // Reparent
            Reparent(newParent, true);

            // Neue local targets relativ zu neuem Parent berechnen
            MovementState.targetPosition = GlobalTransform.AffineInverse() * globalTargetPos;
            MovementState.targetRotation = GlobalTransform.Basis.Inverse() * globalTargetRot;
            MovementState.targetScale = GlobalTransform.Basis.Scale.Inverse() * globalTargetScale;
        }

        public SmoothMovingObject()
        {
            _movementState = new(this);
        }

        public override void _Ready()
        {
            base._Ready();
            RequestReady();
        }
    }
}
