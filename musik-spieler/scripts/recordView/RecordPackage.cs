using Godot;
using System;
using System.Linq;

namespace Musikspieler.Scripts
{
    public partial class RecordPackage : SmoothMovingObject
    {
        [Export] private MeshInstance3D _meshInstance;
        public MeshInstance3D MeshInstance => _meshInstance;

        //Ausblenden ist sinnvoll hier, da, wenn ein Objekt alleine andere Parameter bekommt, die Parameter nicht mehr synchron angepasst werden, was der Sinn der statischen Variable ist.
        public static readonly new SmoothDamp SmoothDamp;

        static RecordPackage()
        {
            //statischer Konstruktor, um die Konstanten zu benennen, und kein "Magic Numbers" zu übergeben, und sie trotzdem nicht in der Klasse herumfliegen zu haben.
            const float PositionSmoothTime = 0.10f;
            const float PositionMaxSpeed = 4000f;
            const float RotationSmoothTime = 0.07f;
            const float RotationMaxSpeed = 40f;
            const float ScaleSmoothTime = 0.10f;
            const float ScaleMaxSpeed = 20f;

            SmoothDamp = new(PositionSmoothTime, PositionMaxSpeed, RotationSmoothTime, RotationMaxSpeed, ScaleSmoothTime, ScaleMaxSpeed);
        }

        ///Im Gegensatz zu Unity kann in Godot mit Konstruktoren gearbeitet werden.
        public RecordPackage()
        {
            base.SmoothDamp = SmoothDamp;
        }
    }
}
