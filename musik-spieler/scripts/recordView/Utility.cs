using Godot;
using Godot.Collections;
using System;
using System.Linq;

namespace Musikspieler.Scripts.RecordView
{
    public class Utility
    {
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        public static bool CameraRaycast(Camera3D camera, Mask<CollisionMask> mask, out Dictionary result)
        {
            if (camera == null)
            {
                GD.Print("no cam");
                result = null;
                return false;
            }

            Vector2 mousePos = camera.GetViewport().GetMousePosition();

            const float rayLength = 1000;

            Vector3 from = camera.ProjectRayOrigin(mousePos);
            Vector3 to = from + camera.ProjectRayNormal(mousePos) * rayLength;

            var spaceState = camera.GetWorld3D().DirectSpaceState;
            result = spaceState.IntersectRay(new PhysicsRayQueryParameters3D
            {
                From = from,
                To = to,
                CollisionMask = mask
            });
            return result.Count > 0;
        }
    }
}
