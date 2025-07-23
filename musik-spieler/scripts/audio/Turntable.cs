using System;
using Godot;

namespace Musikspieler.Scripts.Audio
{
    public interface ITurntable
    {
        float CurrentLoop { get; }
        float CurrentSpeed { get; }
        float CurrentSongPosition { get; }
        float MaxLoops { get; }
        bool IsMotorRunning { get; }

        void StartMotor();
        void StopMotor();
        void ToggleMotor();
        void SetMotorState(bool enabled);
        void ChangeMotorSpeed(float speed);
        void Rotate(float loops);
        void MoveArm(float pos);
        void ScratchTarget(float deltaLoops);
        void EndScratch();
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
        private float lerpSpeed = 10f; // Kalibrierter Wert
        private bool scratchActive = false;
        private float targetLoop = 0f;
        private float lastLoop = 0f;
        public float CurrentLoop => currentLoop;
        public float CurrentSpeed => currentSpeed;
        public float CurrentSongPosition => currentLoop / maxLoops;
        public float MaxLoops => maxLoops;
        public bool IsMotorRunning => motorRunning;

        public void SetMaxLoops(float songLength)
        {
            maxLoops = motorSpeed / 60 * songLength;
        }

        public void SimulationStep(double delta)
        {
            currentSpeed = Mathf.Min(currentSpeed, 1000f);

            if (Mathf.Abs(currentSpeed) == 0.0f)
            {
                motorRunning = !(targetSpeed == 0);
            }

            if (scratchActive)
            {
                float lerpAlpha = 1f - MathF.Exp(-lerpSpeed * (float)delta);
                currentLoop = Mathf.Lerp(currentLoop, targetLoop, lerpAlpha);
                currentSpeed = (currentLoop - lastLoop) / (float)delta;
                lastLoop = currentLoop;
            }
            else
            {
                // exponentieller Drag
                float dragPerStep = MathF.Pow(drag, (float)delta);
                currentSpeed *= dragPerStep;
                currentLoop += currentSpeed * (float)delta;
                targetLoop = currentLoop;
            }

            // Dieses Clamping simuliert ein Springen des Tonarms ("Sprung in der Platte")
            // Es ermöglicht so das freie Drehen der Platte ohne die Animation zu stören
            if (currentLoop < 0)
            {
                currentLoop = 1;
                targetLoop += 1;
            }
            else if (currentLoop >= maxLoops)
            {
                currentLoop = MaxLoops - 1;
                targetLoop -= 1;
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
            currentLoop = Mathf.Clamp(currentLoop, 0, MaxLoops);
            pos = Mathf.Clamp(pos, 0, 1);
            Rotate((int)(pos * maxLoops) - (int)currentLoop);
        }

        public void ScratchTarget(float deltaLoops)
        {
            targetLoop += deltaLoops;
            scratchActive = true;
        }

        public void EndScratch()
        {
            scratchActive = false;
        }

        public void BoostSpeed(float fraction)
        {
            currentSpeed += targetSpeed * fraction;
        }
    }
}
