using Godot;
using System;
using System.Linq;

namespace Scripts
{
    public partial class RecordPackage : SmoothMovingObject
    {
        [Export] private MeshInstance3D _meshInstance;
        public MeshInstance3D MeshInstance => _meshInstance;

        //Ausblenden ist sinnvoll hier, da, wenn ein Objekt alleine andere Parameter bekommt, die Parameter nicht mehr synchron angepasst werden, was der Sinn der statischen Variable ist.
        public static readonly new SmoothDamp SmoothDamp;

        static RecordPackage()
        {
            //statischer Konstruktor, um die Konstanten zu benennen, und kein "Magic Numbers" zu übergeben, und sie trotzdem nicht in der Klasse herumfliegen zu haben.
            const float PositionSmoothTime = 0.10f;
            const float PositionMaxSpeed = 4000f;
            const float RotationSmoothTime = 0.07f;
            const float RotationMaxSpeed = 40f;
            const float ScaleSmoothTime = 0.10f;
            const float ScaleMaxSpeed = 20f;

            SmoothDamp = new(PositionSmoothTime, PositionMaxSpeed, RotationSmoothTime, RotationMaxSpeed, ScaleSmoothTime, ScaleMaxSpeed);
        }

        ///Im Gegensatz zu Unity kann in Godot mit Konstruktoren gearbeitet werden.
        public RecordPackage()
        {
            base.SmoothDamp = SmoothDamp;
        }
    }

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

    /// <summary>
    /// Klasse, die drei Parameter speichert, um SmoothDamp auf allen drei Transform3D-Komponenten auszuführen.
    /// </summary>
    /// Als Klasse (nicht struct), damit mehrere Objekte die gleichen Parameter nutzen können, und Änderungen durch die Referenz sofort Effekt haben.
    public class SmoothDamp
    {
        public SmoothMovementParameters RotationParameters;
        public SmoothMovementParameters PositionParameters;
        public SmoothMovementParameters ScaleParameters;

        public SmoothDamp(SmoothMovementParameters rotationParameters, SmoothMovementParameters positionParameters, SmoothMovementParameters scaleParameters)
        {
            RotationParameters = rotationParameters;
            PositionParameters = positionParameters;
            ScaleParameters = scaleParameters;
        }

        public SmoothDamp(float positionSmoothTime, float positionMaxSpeed, float rotationSmoothTime, float rotationMaxSpeed, float scaleSmoothTime, float scaleMaxSpeed)
        {
            RotationParameters = new(positionSmoothTime, positionMaxSpeed);
            PositionParameters = new(rotationSmoothTime, rotationMaxSpeed);
            ScaleParameters = new(scaleSmoothTime, scaleMaxSpeed);
        }

        // Da SmoothMovementParameters so eng mit SmoothDamp "verwandt" ist, als lokale Klasse deklariert.
        // Somit ist auch der Kontext klarer, und dass diese Klasse nur unter gleichzeitiger Verwendung eines SmoothDamp-Objektes benötigt wird.
        public class SmoothMovementParameters
        {
            public float smoothTime = 0.1f;
            public float maxSpeed = 10f;

            public SmoothMovementParameters(float smoothTime, float maxSpeed)
            {
                this.smoothTime = smoothTime;
                this.maxSpeed = maxSpeed;
            }
        }

        ///Klasse, da die Geschwindigkeiten per Referenz geändert werden müssen. Structs würden das verhindern.
        public class SmoothMovementState
        {
            public Vector3 targetRotation;
            public Vector3 targetPosition;
            public Vector3 targetScale;
            public Vector3 rotationVelocity;
            public Vector3 positionVelocity;
            public Vector3 scaleVelocity;

            /// <summary>
            /// Um das Smoothing pausieren auf Per-Object-Basis.
            /// </summary>
            public bool disableSmoothing;

            public SmoothMovementState(Node3D transformToCopy)
            {
                targetRotation = transformToCopy.Rotation;
                targetPosition = transformToCopy.Position;
                targetScale = transformToCopy.Scale;
            }
        }

        public void Step(Node3D node, SmoothMovementState state, float deltaTime)
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(state);

            if (state.disableSmoothing)
            {
                node.Position = state.targetPosition;
                node.Rotation = state.targetRotation;
                node.Scale = state.targetScale;
                return;
            }

            if (PositionParameters != null)
                node.Position = Step(node.Position, state.targetPosition, ref state.positionVelocity, PositionParameters.smoothTime, PositionParameters.maxSpeed, deltaTime);

            if (RotationParameters != null)
                node.Rotation = Step(node.Rotation, state.targetRotation, ref state.rotationVelocity, RotationParameters.smoothTime, RotationParameters.maxSpeed, deltaTime);

            if (ScaleParameters != null)
                node.Scale = Step(node.Scale, state.targetScale, ref state.scaleVelocity, ScaleParameters.smoothTime, ScaleParameters.maxSpeed, deltaTime);
        }


        /// <summary>
        /// Eine Glättungsfunktion, um eine Zielposition über Zeit mit glatter Bewegung zu erreichen.
        /// <para>
        /// Falls mehrere Achsen gleichzeitig bewegt werden, wird dringend empfohlen, die entsprechende Überladung zu verwenden, da bei diagonaler Bewegung sonst Geschwindigkeiten über der mit maxSpeed gesetzten Maximalgeschwindigkeiten entstehen können, bzw. unrealistisches Verhalten im allgemeinen.
        /// </para>
        /// </summary>
        /// <param name="current">Die aktuelle PositionParameters</param>
        /// <param name="target">Die Zielposition</param>
        /// <param name="currentVelocity">Die aktuelle Geschwindigkeit. Diese Variable sollte pro bewegtes Objekt spezifisch sein, und außerhalb dieser Funktion nicht geändert werden.</param>
        /// <param name="smoothTime">In welcher Zeit der Schritt passieren soll. Höhere Werte erzeugen eine höhere Beschleunigung.</param>
        /// <param name="maxSpeed">Maximale Geschwindigkeit der Bewegung.</param>
        /// <param name="deltaTime">In welcher Zeit die Bewegung passiert. Hier sollte immer die deltaTime des Update-Frames anliegen.</param>
        /// <returns>Die veränderte neue PositionParameters.</returns>
        public static float Step(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float maxChange = maxSpeed * smoothTime;
            float change = Mathf.Clamp(current - target, -maxChange, maxChange);
            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float output = current - change + (change + temp) * exp;
            if ((target - current > 0f) == (output > target))
            {
                output = target;
                currentVelocity = (output - target) / deltaTime;
            }
            return output;
        }

        /// <summary>
        /// Eine Glättungsfunktion, um eine Zielposition über Zeit mit glatter Bewegung zu erreichen.
        /// </summary>
        /// <param name="current">Die aktuelle PositionParameters</param>
        /// <param name="target">Die Zielposition</param>
        /// <param name="currentVelocity">Die aktuelle Geschwindigkeit. Diese Variable sollte pro bewegtes Objekt spezifisch sein, und außerhalb dieser Funktion nicht geändert werden.</param>
        /// <param name="smoothTime">In welcher Zeit der Schritt passieren soll. Höhere Werte erzeugen eine höhere Beschleunigung.</param>
        /// <param name="maxSpeed">Maximale Geschwindigkeit der Bewegung.</param>
        /// <param name="deltaTime">In welcher Zeit die Bewegung passiert. Hier sollte immer die deltaTime des Update-Frames anliegen.</param>
        /// <returns>Die veränderte neue PositionParameters.</returns>
        public static Vector2 Step(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float maxChange = maxSpeed * smoothTime;
            Vector2 change = target - current;
            float changeLength = Mathf.Max(change.Length(), 0.000001f);
            change *= Mathf.Min(changeLength, maxChange) / changeLength;
            Vector2 temp = (currentVelocity - omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            Vector2 output = current + change + (temp - change) * exp;
            if (change.Dot(target - output) < 0)
            {
                output = target;
                currentVelocity = (output - target) / deltaTime;
            }
            return output;
        }

        /// <summary>
        /// Eine Glättungsfunktion, um eine Zielposition über Zeit mit glatter Bewegung zu erreichen.
        /// </summary>
        /// <param name="current">Die aktuelle PositionParameters</param>
        /// <param name="target">Die Zielposition</param>
        /// <param name="currentVelocity">Die aktuelle Geschwindigkeit. Diese Variable sollte pro bewegtes Objekt spezifisch sein, und außerhalb dieser Funktion nicht geändert werden.</param>
        /// <param name="smoothTime">In welcher Zeit der Schritt passieren soll. Höhere Werte erzeugen eine höhere Beschleunigung.</param>
        /// <param name="maxSpeed">Maximale Geschwindigkeit der Bewegung.</param>
        /// <param name="deltaTime">In welcher Zeit die Bewegung passiert. Hier sollte immer die deltaTime des Update-Frames anliegen.</param>
        /// <returns>Die veränderte neue PositionParameters.</returns>
        public static Vector3 Step(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float maxChange = maxSpeed * smoothTime;
            Vector3 change = target - current;
            float changeLength = Mathf.Max(change.Length(), 0.000001f);
            change *= Mathf.Min(changeLength, maxChange) / changeLength;
            Vector3 temp = (currentVelocity - omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            Vector3 output = current + change + (temp - change) * exp;
            if (change.Dot(target - output) < 0)
            {
                output = target;
                currentVelocity = (output - target) / deltaTime;
            }
            return output;
        }
    }

    public class Utility
    {
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }
    }
}

