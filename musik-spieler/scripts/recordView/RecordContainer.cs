using Godot;
using System;

namespace Musikspieler.Scripts
{
    public partial class RecordContainer : SmoothMovingObject
    {
        //Ausblenden ist sinnvoll hier, da, wenn ein Objekt alleine andere Parameter bekommt, die Parameter nicht mehr synchron angepasst werden, was der Sinn der statischen Variable ist.
        public static readonly new SmoothDamp SmoothDamp;

        static RecordContainer()
        {
            //statischer Konstruktor, um die Konstanten zu benennen, und kein "Magic Numbers" zu übergeben, und sie trotzdem nicht in der Klasse herumfliegen zu haben.
            const float PositionSmoothTime = 0.1f;
            const float PositionMaxSpeed = 40f;

            SmoothDamp = new(new(PositionSmoothTime, PositionMaxSpeed), null, null);
        }

        ///Im Gegensatz zu Unity kann in Godot mit Konstruktoren gearbeitet werden.
        public RecordContainer()
        {
            base.SmoothDamp = SmoothDamp;
        }
    }
}
