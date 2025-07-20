using Godot;

namespace Musikspieler.Scripts.RecordView
{
    //Als lokale Objkte deklariert, damit klar ist für was sie gedacht sind.
    public abstract partial class ScrollView<T> : View where T : IItem
    {
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

        public struct NoAnimationXAnimationFunction : IAnimationXFunction
        {
            public readonly float ForwardGapToViewBoundryMargin => 0.5f;
            public readonly float BackwardGapToViewBoundryMargin => 0.5f;

            public readonly float AnimationFunction(Vector2 relativeMousePos) => 0f;
        }

        public struct NoAnimationYAnimationFunction : IAnimationYFunction
        {
            public readonly float AnimationFunction(Vector2 relativeMousePos) => 0f;
        }
    }
}