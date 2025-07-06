using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public partial class RecordView : Node3D
{
	public PackedScene recordPrefab = GD.Load<PackedScene>("res://record_package.tscn");

    private class Song(string name)
    {
        public readonly string name = name;
    }

    private float movableContainerVelocity;

    private float _movableContainerTarget;
    private float MovableContainerTarget
    {
        get => _movableContainerTarget;
        set
        {
            float size = (((BoxShape3D)mousePlane.Shape).Size.Z - ((BoxShape3D)recordViewBounds.Shape).Size.Z) * 0.5f;      //da mousePlane sich anpasst
            value = Math.Clamp(value, -size, size);
            _movableContainerTarget = value;
        }
    }

    private readonly List<RecordPackage> currentlyDraggedPackages = new();

    private struct RecordPackageSlot
    {
        public int index;       //has to be updated if list changes
        public RecordPackage packageObject; //can be null if outside the window! changes dynamically.
        public Song song;   //muss eigentlich zu ReckordPackage verschoben werden
    }

	private readonly List<RecordPackageSlot> recordPackageObjects = [];

    [Export] float gapWidth = 4.0f;
    [Export] float backSideOffset = 2.5f;
    [Export] CollisionShape3D mousePlane;
    [Export] Node3D recordsContainer;
    [Export] Node3D movableContainer;
    [Export] CollisionShape3D recordViewBounds;
    [Export] ShaderMaterial BaseMaterial;

    private ShaderMaterial instancedMaterial;

    private int lastGapIndex = -1;

    private const float recordPackageWidth = 0.25f;

    private int gapIndex;

    private float unconsumedScrollDelta;

    public override void _Ready()
    {
        base._Ready();
		RequestReady();         //so that _ready() will be called again for the next instance of this class
		Init();
    }

	private void Init()
    {
        //just for testing
        List<Song> playlistSongs = new(100);
        for (int i = 0; i < 100; i++)
        {
            playlistSongs.Add(new Song(RandomString(10)));
        }

        if (mousePlane == null)
            throw new Exception("no collision shape");
        if (recordsContainer == null)
            throw new Exception("no records container");

        for (int i = 0; i < playlistSongs.Count; i++)
		{
            RecordPackage record = (RecordPackage)recordPrefab.Instantiate();
            record.Name = $"RecordPackage_{i}";
            recordsContainer.AddChild(record);       //adds the instanciated object to the scene, makes it visible
            recordPackageObjects.Add(new RecordPackageSlot() { index = i, packageObject = record, song = playlistSongs[i] });
        }

        SetPlaylistSize();

        // Material instanziieren
        instancedMaterial = (ShaderMaterial)BaseMaterial.Duplicate();

        var nodes = recordsContainer.GetChildren();

        foreach (var node in nodes)
        {
            if (node is MeshInstance3D mesh)
            {
                mesh.MaterialOverride = instancedMaterial;
            }
        }

        GD.Print($"Ich bin RecordView: {Name} / ID: {GetInstanceId()}");
    }

    private void SetPlaylistSize()
    {
        const float clickPlaneWidth = 2.5f;

        float margin = ((BoxShape3D)recordViewBounds.Shape).Size.Z;

        float size = recordPackageWidth * recordPackageObjects.Count + margin;
        ((BoxShape3D)mousePlane.Shape).Size = new Vector3(clickPlaneWidth, 0.01f, size);
        recordsContainer.Position = new Vector3(0, 0, -size * 0.5f);

        for (int i = 0; i < recordPackageObjects.Count; i++)
        {
            recordPackageObjects[i] = new RecordPackageSlot()
            {
                packageObject = recordPackageObjects[i].packageObject,
                song = recordPackageObjects[i].song,
                index = i,
            };
        }
    }

    private Dictionary CameraRaycast(uint mask)
    {
        var camera = GetViewport().GetCamera3D();

        if (camera == null)
        {
            GD.Print("no cam");
            return null;
        }

        Vector2 mousePos = GetViewport().GetMousePosition();

        Vector3 from = camera.ProjectRayOrigin(mousePos);
        Vector3 to = from + camera.ProjectRayNormal(mousePos) * 1000;

        var spaceState = GetWorld3D().DirectSpaceState;
        return spaceState.IntersectRay(new PhysicsRayQueryParameters3D
        {
            From = from,
            To = to,
            CollisionMask = mask
        });
    }

    public Vector3? GetDraggingMousePos()
    {
        var result = CameraRaycast(4);

        if (result == null)
            return null;

        if (result.Count > 0)
        {
            return (Vector3)result["position"];
        }
        else return null;
    }

	private Vector2? GetRelativeMousePos()
	{
        var result = CameraRaycast(8);

        if (result == null || result.Count == 0)
            return null;

        result = CameraRaycast(2);

        if (result == null)
            return null;

        if (result.Count > 0 && (Node3D)result["collider"] == mousePlane.GetParent())
        {
            Vector3 hitPos = (Vector3)result["position"];

            Vector3 localPos = mousePlane.GlobalTransform.AffineInverse() * hitPos;
            return new Vector2(localPos.X, localPos.Z + ((BoxShape3D)mousePlane.Shape).Size.Z * 0.5f);
        }
        else return null;
    }

    //Man könnte das Updaten des Zielzustands auch entkoppelter mit Events lösen, jedoch macht das wenig Sinn, da sie sowieso von hier gemanaged sind, und keine eigenständigen Objekte sind.
    //Wären sie das, könnt evtl. nicht sichergestellt werden, 
    /// <summary>
    /// Die Schallplatten-Packungen haben einen TranformTarget, also einen Zielzustand (Translation und Rotation) den sie erreichen sollen. Hier wird dieser Zustand neu gesetzt.
    /// </summary>
    private void UpdatePackageTransformTargets(RecordPackageSlot packageSlot, Vector2? mousePos, out float mouseDst)
    {
        float maxYAngle = Mathf.DegToRad(6);
        float maxXAngle = Mathf.DegToRad(50);

        float xRotation; 
        float yRotation;

        if (currentlyDraggedPackages.Contains(packageSlot.packageObject))
        {
            mouseDst = float.NaN;
            return;
        }

        float startMargin = ((BoxShape3D)recordViewBounds.Shape).Size.Z * 0.5f;
        packageSlot.packageObject.targetPosition = new(0, 0, startMargin + packageSlot.index * recordPackageWidth);

        mousePos += new Vector2(0, 0.6f);

        if (mousePos.HasValue)
        {
            Vector2 packageToMouse = mousePos.Value - new Vector2(packageSlot.packageObject.Position.X, packageSlot.packageObject.Position.Z);
            Vector2 packageToMouseNormalized = packageToMouse.Normalized();
            mouseDst = packageToMouse.Y;
            if (mouseDst < 0) mouseDst -= backSideOffset;
            mouseDst = Mathf.Clamp(mouseDst, -gapWidth, gapWidth);
            xRotation = -0.5f * (Mathf.Cos(Mathf.Pi / gapWidth * mouseDst) + 1) * Mathf.Sign(mouseDst) * maxXAngle;
            yRotation = Mathf.Min(Mathf.Abs(packageToMouseNormalized.X) / (100 * Mathf.Max(packageToMouse.Length(), 0.3f)), maxYAngle) * Mathf.Sign(packageToMouseNormalized.Y * packageToMouseNormalized.X);
            mouseDst = packageToMouse.Y;
        }
        else
        {
            xRotation = 0;
            yRotation = 0;
            mouseDst = float.NaN;
        }

        packageSlot.packageObject.targetRotation = new Vector3(xRotation, yRotation, 0);
    }

	public override void _Process(double delta)
	{
		Vector2? mousePos = GetRelativeMousePos();

        gapIndex = -10;
        float minDstToMouse = float.MaxValue;
        for (int i = 0; i < recordPackageObjects.Count; i++)
        {
            UpdatePackageTransformTargets(recordPackageObjects[i], mousePos, out float mouseDst);
            if (float.IsNaN(mouseDst))
                continue;
            if (mouseDst >= 0)
            {
                if (minDstToMouse < 0 || (minDstToMouse > 0 && mouseDst < minDstToMouse))
                {
                    minDstToMouse = mouseDst;
                    gapIndex = i;
                }
            }
            else       //wenn vor dem gap nix mehr ist, finde die naheliegenste dahinterliegede packung
            {
                if (gapIndex == -10 || (minDstToMouse < 0 && mouseDst > minDstToMouse))
                {
                    minDstToMouse = mouseDst;
                    gapIndex = i - 1;
                }
            }
        }

        const float scrollSensitivity = -0.9f;

        MovableContainerTarget += unconsumedScrollDelta * scrollSensitivity;

        const float movableContainerSmoothTime = 0.1f;
        const float movableContainerMaxSpeed = 40f;

        float newZ = Utility.SmoothDamp(movableContainer.Position.Z, MovableContainerTarget, ref movableContainerVelocity, movableContainerSmoothTime, movableContainerMaxSpeed, (float)delta);
        movableContainer.Position = new Vector3(movableContainer.Position.X, movableContainer.Position.Y, newZ);

        unconsumedScrollDelta = 0;

        if (currentlyDraggedPackages.Count > 0)
        {
            Vector3? globalMousePos = GetDraggingMousePos();

            if (!globalMousePos.HasValue)
                return;

            foreach (var package in currentlyDraggedPackages)
            {
                package.targetPosition = globalMousePos.Value;
            }
        }
        instancedMaterial.SetShaderParameter("box_transform", recordViewBounds.GlobalTransform);
        instancedMaterial.SetShaderParameter("box_size", ((BoxShape3D)recordViewBounds.Shape).Size);

        if (pendingPackages.Count > 0)
        {
            var package = pendingPackages[0];
            if ((package.Position - package.targetPosition).LengthSquared() < 0.4f)
            {
                pendingPackages.Remove(package);
                package.MaterialOverride = instancedMaterial;
            }
        }
    }


    /// <summary>
    /// packages that are being put in the playlist, but have get close enough to so we can change the shader bounds.
    /// </summary>
    private List<RecordPackage> pendingPackages = new();

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            //mouseEvent.Pressed == true, da godot srollen auch als drücken und loslassen interpretiert, d.h. wie bekommen sonst alles doppelt 0_0
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed)
            {
                if (GetRelativeMousePos() != null)
                    unconsumedScrollDelta--;
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed)
            {
                if (GetRelativeMousePos() != null)
                    unconsumedScrollDelta++;
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (mouseEvent.Pressed)
                {
                    if (gapIndex < 0)
                        return;

                    lastGapIndex = gapIndex;

                    var package = recordPackageObjects[gapIndex].packageObject;
                    currentlyDraggedPackages.Add(package);
                    recordPackageObjects.RemoveAt(gapIndex);
                    package.Reparent(GetTree().Root, true);
                    package.Teleport(package.Position, package.Rotation);
                    package.MaterialOverride = BaseMaterial;
                }
                else
                {
                    if (currentlyDraggedPackages.Count == 0)
                        return;
                    
                    int gapIndex = this.gapIndex + 1;
                    
                    if (gapIndex < 0)
                    {
                        gapIndex = lastGapIndex;
                    }

                    var package = currentlyDraggedPackages.First();
                    recordPackageObjects.Insert(gapIndex, new RecordPackageSlot() { packageObject = package, index = gapIndex });
                    currentlyDraggedPackages.Remove(package);
                    package.Reparent(recordsContainer, true);
                    package.Teleport(package.Position, package.Rotation);
                    SetPlaylistSize();
                    pendingPackages.Add(package);
                }
            }
        }
    }

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}
