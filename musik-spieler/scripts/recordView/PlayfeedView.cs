using Godot;
using System;

namespace Musikspieler.Scripts.RecordView
{
    public partial class PlayfeedView : ScrollView
    {
        protected override bool AcceptItem(ViewItem item)
        {
            if (item is Drawer drawer)
            {
                GD.Print("added drawer in playfeedview");
                return true;
            }
            if (item is RecordPackage recordPackage)
            {
                GD.Print("added record package in playfeedview");
                return true;
            }
            return false;
        }
    }
}