namespace Musikspieler.Scripts.RecordView
{
    //als Zwischenlayer, damit der GrabHandler ViewItems jeglichen Typs anfassen kann
    public abstract partial class ViewItem : SmoothMovingObject
    {
        /// <summary>
        /// An welchem Index diese Packung gerade in seinem ChildView liegt. Wenn die Packung herumgezogen wird, zeigt der Index immer noch auf die Stelle, wo es herkam.
        /// </summary>
        public int ViewIndex { get; protected set; }

        /// <summary>
        /// Ob die Packung sich gerade ausserhalb des Views bewegt. Immer true, wenn isGettingDragged true ist, aber auch, wenn noch Animationen abgespielt werden nach dem Loslassen.
        /// </summary>
        public bool IsPending { get; protected set; }

        /// <summary>
        /// Ob die Packung gerade herumgezogen wird. Wird direkt auf false gesetzt, wenn der Nutzer loslässt.
        /// </summary>
        public abstract bool IsGettingDragged { get; set; }

        public abstract bool Move(View targetView);
    }
}