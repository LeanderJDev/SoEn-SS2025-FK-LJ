using Godot;
using System.Collections.Generic;

namespace Musikspieler.Scripts.RecordView
{
    public partial class RecordView : ScrollView<ISong>
    {
        public override void _Ready()
        {
            base._Ready();

            //Animationfunctions setzen
            Animation = new Animations(
                forwardMargin: 0.3f,
                backwardMargin: 0.9f,
                Animations.BinaryFlickThroughRotationXAnimationFunction,
                Animations.SubtleRotationYAnimationFunction
            );

            itemObjectWidth = 0.25f;
            scrollAreaSize = 0.3f;
            flipThresholdOffset = -0.2f;
            flipThreshold = 1.7f;


            /*
            //NUR FÜR TESTZWECKE
            GD.Print("RecordView created");
            List<ISong> songs = new(100);
            for (int i = 0; i < 100; i++)
            {
                songs.Add(new Song(Utility.RandomString(10)));
            }
            ItemList playlist = new("ItemList");
            ItemList = playlist;
            playlist.AddItems(songs);
            */
        }
    }
}