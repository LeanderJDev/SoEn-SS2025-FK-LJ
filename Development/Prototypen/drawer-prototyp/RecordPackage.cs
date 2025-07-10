using Godot;
using System;
using System.Linq;
using Utilities;

public partial class RecordPackage : MeshInstance3D
{
    public Vector3 targetRotation;
    public Vector3 targetPosition;

    private Vector3 translationVelocity;
    private Vector3 rotationVelocity;

    public override void _Process(double delta)
    {
        base._Process(delta);

        const float rotationSmoothTime = 0.07f;
        const float rotationMaxSpeed = 40f;

        const float translationSmoothTime = 0.10f;
        const float translationMaxSpeed = 4000f;

        Rotation = SmoothDamp.Step(Rotation, targetRotation, ref rotationVelocity, rotationSmoothTime, rotationMaxSpeed, (float)delta);
        Position = SmoothDamp.Step(Position, targetPosition, ref translationVelocity, translationSmoothTime, translationMaxSpeed, (float)delta);
    }

    public void Teleport(Vector3 pos, Vector3 rot)
    {
        targetPosition = pos;
        targetRotation = rot;
        Position = pos;
        Rotation = rot;
    }
}

namespace Utilities
{
    public static class SmoothDamp
    {
        /// <summary>
        /// Eine Glättungsfunktion, um eine Zielposition über Zeit mit glatter Bewegung zu erreichen.
        /// <para>
        /// Falls mehrere Achsen gleichzeitig bewegt werden, wird dringend empfohlen, die entsprechende Überladung zu verwenden, da bei diagonaler Bewegung sonst Geschwindigkeiten über der mit maxSpeed gesetzten Maximalgeschwindigkeiten entstehen können, bzw. unrealistisches Verhalten im allgemeinen.
        /// </para>
        /// </summary>
        /// <param name="current">Die aktuelle Position</param>
        /// <param name="target">Die Zielposition</param>
        /// <param name="currentVelocity">Die aktuelle Geschwindigkeit. Diese Variable sollte pro bewegtes Objekt spezifisch sein, und außerhalb dieser Funktion nicht geändert werden.</param>
        /// <param name="smoothTime">In welcher Zeit der Schritt passieren soll. Höhere Werte erzeugen eine höhere Beschleunigung.</param>
        /// <param name="maxSpeed">Maximale Geschwindigkeit der Bewegung.</param>
        /// <param name="deltaTime">In welcher Zeit die Bewegung passiert. Hier sollte immer die deltaTime des Update-Frames anliegen.</param>
        /// <returns>Die veränderte neue Position.</returns>
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
        /// <param name="current">Die aktuelle Position</param>
        /// <param name="target">Die Zielposition</param>
        /// <param name="currentVelocity">Die aktuelle Geschwindigkeit. Diese Variable sollte pro bewegtes Objekt spezifisch sein, und außerhalb dieser Funktion nicht geändert werden.</param>
        /// <param name="smoothTime">In welcher Zeit der Schritt passieren soll. Höhere Werte erzeugen eine höhere Beschleunigung.</param>
        /// <param name="maxSpeed">Maximale Geschwindigkeit der Bewegung.</param>
        /// <param name="deltaTime">In welcher Zeit die Bewegung passiert. Hier sollte immer die deltaTime des Update-Frames anliegen.</param>
        /// <returns>Die veränderte neue Position.</returns>
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
        /// <param name="current">Die aktuelle Position</param>
        /// <param name="target">Die Zielposition</param>
        /// <param name="currentVelocity">Die aktuelle Geschwindigkeit. Diese Variable sollte pro bewegtes Objekt spezifisch sein, und außerhalb dieser Funktion nicht geändert werden.</param>
        /// <param name="smoothTime">In welcher Zeit der Schritt passieren soll. Höhere Werte erzeugen eine höhere Beschleunigung.</param>
        /// <param name="maxSpeed">Maximale Geschwindigkeit der Bewegung.</param>
        /// <param name="deltaTime">In welcher Zeit die Bewegung passiert. Hier sollte immer die deltaTime des Update-Frames anliegen.</param>
        /// <returns>Die veränderte neue Position.</returns>
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

