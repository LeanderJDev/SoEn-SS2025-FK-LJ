using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musikspieler.Scripts.RecordView
{
    public partial class RaycastHandler : Node3D
    {
        public const float raycastQueryLength = 1000f;

        public static Mask<CollisionMask> Mask { get; private set; }

        private static Viewport viewport;
        private static Camera3D camera;

        public RaycastHandler()
        {
            var mask = Mask<CollisionMask>.All();
            mask.Remove(CollisionMask.GlobalDragPlane);
            Mask = mask;
        }

        public struct RaycastHit
        {
            public CollisionObject3D collider;
            public Vector3 pos;
        }

        //kann von außen manipuliert werden, aber das kann auch nützlich sein
        public static List<RaycastHit> RaycastHits { get; private set; } = [];

        public override void _Process(double delta)
        {
            RaycastHits = RaycastQuery();
            base._Process(delta);
        }

        public static bool IsObjectUnderCursor(CollisionObject3D collider)
        {
            return RaycastHits.Exists(x => x.collider == collider);
        }

        public static bool IsObjectUnderCursor(CollisionObject3D collider, out Vector3 hitPosition)
        {
            RaycastHit hit = RaycastHits.FirstOrDefault(x => x.collider == collider);
            hitPosition = hit.pos;
            return hit.collider != null;
        }

        private List<RaycastHit> RaycastQuery()
        {
            viewport ??= GetViewport();

            if (viewport == null)
            {
                GD.PrintErr("no viewport");
                return [];
            }


            camera ??= viewport.GetCamera3D();

            if (camera == null)
            {
                GD.PrintErr("no camera");
                return [];
            }

            Vector2 mousePos = viewport.GetMousePosition();

            Vector3 from = camera.ProjectRayOrigin(mousePos);
            Vector3 to = from + camera.ProjectRayNormal(mousePos) * raycastQueryLength;

            var spaceState = camera.GetWorld3D().DirectSpaceState;

            List<Rid> excludes = [];
            List<RaycastHit> hits = [];

            do
            {
                var query = new PhysicsRayQueryParameters3D
                {
                    From = from,
                    To = to,
                    Exclude = new(excludes),
                    CollisionMask = Mask,
                };

                var result = spaceState.IntersectRay(query);

                if (result == null)
                    break;

                if (result.Count <= 0)
                    break;

                CollisionObject3D collider = (CollisionObject3D)(Node)result["collider"];
                Vector3 pos = (Vector3)result["position"];

                hits.Add(new()
                {
                    pos = pos,
                    collider = collider,
                });

                excludes.Add(collider.GetRid());
            }
            while (true);

            return hits;
        }

        public static float MouseToTargetAngle(Node3D target, Vector2 mousePos)
        {
            // 1. Ray von Kamera durch Maus
            float targetY = target.GlobalPosition.Y;
            Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
            Vector3 rayDir = camera.ProjectRayNormal(mousePos);

            // 2. Schnittpunkt mit Platten-Ebene (z.B. y = Plattenhöhe)
            float t = (targetY - rayOrigin.Y) / rayDir.Y;
            Vector3 hit = rayOrigin + rayDir * t;

            // 3. Berechne Winkel zum Zentrum des target
            Vector3 center = target.GlobalTransform.Origin;
            Vector3 dir = (hit - center).Normalized();
            float angle = Mathf.Atan2(dir.X, dir.Z); // Winkel zur Z-Achse

            // TODO Verstehen was hier los ist und Magische Variablen entfernen
            return angle;
        }
    }
}
