using Godot;
using Godot.NativeInterop;
using System;

public partial class RecordPackage : MeshInstance3D
{
    public Vector3 targetRotation;
    public Vector3 targetPosition;

    private float xRotationVelocity;
    private float yRotationVelocity;
    private float zRotationVelocity;

    private float xTranslationVelocity;
    private float yTranslationVelocity;
    private float zTranslationVelocity;

    public override void _Process(double delta)
    {
        base._Process(delta);

        const float rotationSmoothTime = 0.07f;
        const float rotationMaxSpeed = 40f;

        const float translationSmoothTime = 0.10f;
        const float translationMaxSpeed = 4000f;

        float smoothedX = Utility.SmoothDamp(Rotation.X, targetRotation.X, ref xRotationVelocity, rotationSmoothTime, rotationMaxSpeed, (float)delta);
        float smoothedY = Utility.SmoothDamp(Rotation.Y, targetRotation.Y, ref yRotationVelocity, rotationSmoothTime, rotationMaxSpeed, (float)delta);
        float smoothedZ = Utility.SmoothDamp(Rotation.Z, targetRotation.Z, ref zRotationVelocity, rotationSmoothTime, rotationMaxSpeed, (float)delta);
        Rotation = new Vector3(smoothedX, smoothedY, smoothedZ);

        smoothedX = Utility.SmoothDamp(Position.X, targetPosition.X, ref xTranslationVelocity, translationSmoothTime, translationMaxSpeed, (float)delta);
        smoothedY = Utility.SmoothDamp(Position.Y, targetPosition.Y, ref yTranslationVelocity, translationSmoothTime, translationMaxSpeed, (float)delta);
        smoothedZ = Utility.SmoothDamp(Position.Z, targetPosition.Z, ref zTranslationVelocity, translationSmoothTime, translationMaxSpeed, (float)delta);

        Position = new Vector3(smoothedX, smoothedY, smoothedZ);
    }

    public void Teleport(Vector3 pos, Vector3 rot)
    {
        targetPosition = pos;
        targetRotation = rot;
        Position = pos;
        Rotation = rot;
    }
}

public static class Utility
{
    public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
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
}
