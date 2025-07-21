using System;
using Godot;

namespace Musikspieler.Scripts.Audio
{
    public interface ITurntable
    {
        float CurrentLoop { get; }
        float CurrentSpeed { get; }
        float MaxLoops { get; }
        bool IsMotorRunning { get; }

        void StartMotor();
        void StopMotor();
        void ToggleMotor();
        void SetMotorState(bool enabled);
        void ChangeMotorSpeed(float speed);
        void Rotate(float loops);
        void MoveArm(float pos);
        void Scratch(float deltaLoops, float scratchSpeed);
        void BoostSpeed(float fraction);
    }

    public class Turntable : ITurntable
    {
        private float currentLoop = 0;
        private float maxLoops;
        private float motorSpeed = 45f;
        private float currentSpeed = 0;
        private float targetSpeed;
        private bool motorRunning;
        private float motorAcceleration = 2.4f; // Kalibrierter Wert
        private float drag = 0.1f; // Kalibrierter Wert
        public float CurrentLoop => currentLoop;
        public float CurrentSpeed => currentSpeed;
        public float MaxLoops => maxLoops;
        public bool IsMotorRunning => motorRunning;

        public void SetMaxLoops(float songLength)
        {
            maxLoops = motorSpeed / 60 * songLength;
        }

        public float GetCurrentSongPosition()
        {
            return currentLoop / maxLoops;
        }

        public void SimulationStep(double delta)
        {
            // exponentieller Drag
            float dragPerStep = MathF.Pow(drag, (float)delta);
            currentSpeed *= dragPerStep;

            currentSpeed = Mathf.Min(currentSpeed, 1000f);

            if (Mathf.Abs(currentSpeed) == 0.0f)
            {
                motorRunning = !(targetSpeed == 0);
                currentSpeed = 0.0f;
            }

            currentLoop += currentSpeed * (float)delta;
            if (currentLoop >= maxLoops || currentLoop < 0)
            {
                StopMotor();
                currentLoop = Mathf.Clamp(currentLoop, 0, maxLoops);
            }

            if (!motorRunning)
                return;

            // Motor Intertia
            if (Mathf.Abs(currentSpeed - targetSpeed) > 0.0f)
            {
                float sign = Mathf.Sign(targetSpeed - currentSpeed);
                currentSpeed += sign * motorAcceleration * (float)delta;
                // Stabilisieren der Zielgeschwindigkeit
                if (sign != Mathf.Sign(targetSpeed - currentSpeed))
                {
                    currentSpeed = targetSpeed;
                }
            }
        }

        public void StartMotor()
        {
            targetSpeed = motorSpeed / 60f;
        }

        public void StopMotor()
        {
            targetSpeed = 0f;
        }

        public void ToggleMotor()
        {
            if (targetSpeed > 0)
                StopMotor();
            else
                StartMotor();
            motorRunning = !(targetSpeed == 0);
        }

        public void SetMotorState(bool enabled)
        {
            motorRunning = enabled;
            if (motorRunning)
            {
                StartMotor();
            }
            else
            {
                StopMotor();
            }
        }

        public void ChangeMotorSpeed(float speed)
        {
            motorSpeed += speed;
        }

        public void Rotate(float loops)
        {
            currentLoop += loops;
        }

        public void MoveArm(float pos)
        {
            Rotate((int)(pos * maxLoops) - (int)currentLoop);
        }

        public void Scratch(float deltaLoops, float scratchSpeed)
        {
            float loopDelta = deltaLoops / (Mathf.Pi * 2);
            currentLoop += loopDelta;
            currentSpeed = scratchSpeed;
        }

        public void BoostSpeed(float fraction)
        {
            currentSpeed += targetSpeed * fraction;
        }
    }
}
