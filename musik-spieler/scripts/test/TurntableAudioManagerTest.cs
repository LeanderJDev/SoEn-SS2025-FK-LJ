using System;
using Godot;
using Musikspieler.Scripts.Audio;

namespace Musikspieler.Scripts.Test
{
    public partial class TurntableAudioManagerTest : Node2D
    {
        [Export]
        public AudioStreamWav sample;

        [Export]
        public TurntableAudioManager turntableAudioManager;


	    private Node2D needle;
        private Node2D record;
        private Vector2 recordCenter;
        private bool isLeftHolding;
        private bool isRightHolding;
        private bool leftMoved = false;
    	private float lastLoop = 0;
        private bool rightDragPreviousMotorState = false;
	    private float lastDragAngle = 0f;


        public override async void _Ready()
        {
            GD.Print("Test readying");
            needle = GetNode<Polygon2D>("Needle");
            record = GetNode<Sprite2D>("Record");
            recordCenter = record.GlobalPosition;

            if (turntableAudioManager == null)
            {
                GD.PrintErr("TurntableAudioManager not found as child!");
                return;
            }
            Song song = new Song("Song", "Album", "Artist", "", sample);
            turntableAudioManager.SetSong(song);
            while (turntableAudioManager.Turntable == null)
            {
                GD.Print("Waiting");
                await ToSignal(GetTree().CreateTimer(1.0), "timeout");
            }
            turntableAudioManager.Turntable.SetMotorState(true);
#if DEBUG
            GD.Print("Test ready");
#endif
        }

        public override void _Process(double delta)
        {
            QueueRedraw();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton btn)
            {
                if (btn.ButtonIndex == MouseButton.Left && btn.Pressed && !isLeftHolding)
                {
                    isLeftHolding = true;
                    leftMoved = false;
                }
                if (btn.ButtonIndex == MouseButton.Left && !btn.Pressed)
                {
                    isLeftHolding = false;
                    if (!leftMoved)
                    {
                        turntableAudioManager.Turntable.ToggleMotor();
                    }
                }
                if (btn.ButtonIndex == MouseButton.Right && btn.Pressed && !isRightHolding)
                {
                    isRightHolding = true;
                    rightDragPreviousMotorState = turntableAudioManager.Turntable.IsMotorRunning;
                    turntableAudioManager.Turntable.SetMotorState(false);
                    Vector2 mousePos = GetViewport().GetMousePosition();
                    Vector2 center = record.GlobalPosition;
                    lastDragAngle = (mousePos - center).Angle();
                }
                if (btn.ButtonIndex == MouseButton.Right && !btn.Pressed)
                {
                    isRightHolding = false;
                    if (rightDragPreviousMotorState)
                    {
                        turntableAudioManager.Turntable.SetMotorState(true);
                            turntableAudioManager.Turntable.BoostSpeed(0.3f);
                    }
                    GD.Print("Drag stopped\n\n");
                }
            }
            if (Input.IsActionPressed("ui_up"))
            {
                turntableAudioManager.Turntable.ChangeMotorSpeed(1);
            }
            if (Input.IsActionPressed("ui_down"))
            {
                turntableAudioManager.Turntable.ChangeMotorSpeed(-1);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            QueueRedraw();
            if (isRightHolding)
            {
                Vector2 mousePos = GetViewport().GetMousePosition();
                float currentAngle = (mousePos - recordCenter).Angle();
                float angleDelta = Mathf.Wrap(currentAngle - lastDragAngle, -Mathf.Pi, Mathf.Pi);
                float scratchSpeed = angleDelta / (2 * Mathf.Pi) / (float)delta;

                lastDragAngle = currentAngle;

                turntableAudioManager.Turntable.Scratch(angleDelta / (2 * Mathf.Pi), scratchSpeed);
                if (Mathf.Abs(scratchSpeed) > 0.001f) GD.Print(scratchSpeed);
            }

            if (isLeftHolding)
            {
                Vector2 mousePos = GetViewport().GetMousePosition();
                GD.Print(mousePos);
                float localMousePos = mousePos.X - Position.X;
                if (localMousePos > 470 && localMousePos < 630)
                {
                    turntableAudioManager.Turntable.MoveArm(1 - (localMousePos - 470) / 160);
                }
                if (Math.Abs(turntableAudioManager.Turntable.CurrentLoop - lastLoop) > 0.5f)
                    leftMoved = true;
            }

            lastLoop = turntableAudioManager.Turntable.CurrentLoop;
        }

        private Font _defaultFont = ThemeDB.FallbackFont;

        public override void _Draw()
        {
            record.Rotation = turntableAudioManager.Turntable.CurrentLoop % 1 * Mathf.Pi * 2;
            needle.Position = new Vector2((1 - (turntableAudioManager.Turntable.CurrentLoop / turntableAudioManager.Turntable.MaxLoops)) * 160 + 470, 294);
            string info = $"CurrentLoop: {turntableAudioManager.Turntable.CurrentLoop:F5} | CurrentSpeed {turntableAudioManager.Turntable.CurrentSpeed:F3} | MotorRunning: {turntableAudioManager.Turntable.IsMotorRunning}";
            DrawString(_defaultFont, new Vector2(100, 30), info, HorizontalAlignment.Center);
        }
    }
}
