using Godot;
using System.Collections.Generic;

namespace Musikspieler.Scripts.RecordView
{
    //Als lokale Objkte deklariert, damit klar ist für was sie gedacht sind.
    public abstract partial class ScrollView<T> : View where T : IItem
    {
        public struct AnimationOutput
        {
            public Vector3 PositionOffset;
            public Vector3 RotationOffset;
            public Vector3 ScaleOffset = Vector3.One;

            public static AnimationOutput operator +(AnimationOutput output1, AnimationOutput output2)
            {
                return new()
                {
                    PositionOffset = output1.PositionOffset + output2.PositionOffset,
                    RotationOffset = output1.RotationOffset + output2.RotationOffset,
                    ScaleOffset = output1.ScaleOffset * output2.ScaleOffset,
                };
            }

            public AnimationOutput() { }
        }
        
        public struct AnimationInput
        {
            public Vector2 relativeMousePos;
            public float PackagePos;
            public bool isSelected;
            public int index;
        }

        public readonly struct Animations(float forwardMargin, float backwardMargin, params Animations.AnimationFunction[] functions)
        {
            public delegate AnimationOutput AnimationFunction(AnimationInput output);
            private readonly AnimationFunction[] functions = functions;

            /// <summary>
            /// Wie nah darf die Gap an den Rand des RecordViews kommen? Um zu vermeiden, dass das Aktuell offene RecordPackage nur halb zu sehen ist.
            /// Da das vom Winkel des Packages abhängt muss es hier definiert sein.
            /// Zur positiven Seite hin.
            /// </summary>
            public readonly float ForwardGapToViewBoundryMargin = forwardMargin;

            /// <summary>
            /// Wie nah darf die Gap an den Rand des RecordViews kommen? Um zu vermeiden, dass das Aktuell offene RecordPackage nur halb zu sehen ist.
            /// Da das vom Winkel des Packages abhängt muss es hier definiert sein.
            /// Zur negativen Seite hin.
            /// </summary>
            public readonly float BackwardGapToViewBoundryMargin = backwardMargin;

            public readonly AnimationOutput RunAnimationFrame(AnimationInput input)
            {
                AnimationOutput output = new();
                for (int i = 0; i < functions.Length; i++)
                {
                    output += functions[i](input);
                }
                return output;
            }

            public static AnimationOutput BinaryFlickThroughRotationXAnimationFunction(AnimationInput input)
            {
                float maxXAngle = Mathf.DegToRad(50);

                float xAngle = input.relativeMousePos.Y < 0 ? maxXAngle * 0.4f : -maxXAngle;

                return new()
                {
                    RotationOffset = new Vector3(xAngle, 0, 0)
                };
            }

            public static AnimationOutput LeaningFlickThroughRotationXAnimationFunction(AnimationInput input)
            {
                float maxXAngle = Mathf.DegToRad(50);
                const float gapWidth = 4.0f;
                const float backSideOffset = 2.5f;

                if (input.relativeMousePos.Y < 0) input.relativeMousePos.Y -= backSideOffset;
                input.relativeMousePos.Y = Mathf.Clamp(input.relativeMousePos.Y, -gapWidth, gapWidth);

                float xAngle = -0.5f * (Mathf.Cos(Mathf.Pi / gapWidth * input.relativeMousePos.Y) + 1) * Mathf.Sign(input.relativeMousePos.Y) * maxXAngle;

                return new()
                {
                    RotationOffset = new Vector3(xAngle, 0, 0)
                };
            }

            public static AnimationOutput SubtleRotationYAnimationFunction(AnimationInput input)
            {
                float maxYAngle = Mathf.DegToRad(6);

                Vector2 vNorm = input.relativeMousePos.Normalized();

                float yAngle = Mathf.Min(Mathf.Abs(vNorm.X) / (100 * Mathf.Max(input.relativeMousePos.Length(), 0.3f)), maxYAngle) * Mathf.Sign(vNorm.Y * vNorm.X);

                return new()
                {
                    RotationOffset = new Vector3(0, yAngle, 0)
                };
            }

            public static AnimationOutput GapOffsetXAnimationFunction(AnimationInput input)
            {
                const float offset = 0.5f;

                float xOffset = input.isSelected ? offset : 0;

                return new()
                {
                    PositionOffset = new Vector3(xOffset, 0, 0)
                };
            }
        }
    }
}