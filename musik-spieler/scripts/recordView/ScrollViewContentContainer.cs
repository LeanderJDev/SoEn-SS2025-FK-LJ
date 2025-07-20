using Godot;
using System;

namespace Musikspieler.Scripts.RecordView
{
    public partial class ScrollViewContentContainer : SmoothMovingObject
    {
        //Ausblenden ist sinnvoll hier, da, wenn ein Objekt alleine andere Parameter bekommt, die Parameter nicht mehr synchron angepasst werden, was der Sinn der statischen Variable ist.
        public static readonly new SmoothDamp SmoothDamp;

        static ScrollViewContentContainer()
        {
            //statischer Konstruktor, um die Konstanten zu benennen, und kein "Magic Numbers" zu übergeben, und sie trotzdem nicht in der Klasse herumfliegen zu haben.
            const float PositionSmoothTime = 0.1f;
            const float PositionMaxSpeed = 100f;

            SmoothDamp = new(positionParameters: new SmoothDamp.SmoothMovementParameters(PositionSmoothTime, PositionMaxSpeed), null, null);
        }

        ///Im Gegensatz zu Unity kann in Godot mit Konstruktoren gearbeitet werden.
        public ScrollViewContentContainer()
        {
            base.SmoothDamp = SmoothDamp;
        }

        public override void _Ready()
        {
            base._Ready();
            RequestReady();
        }
    }
}
