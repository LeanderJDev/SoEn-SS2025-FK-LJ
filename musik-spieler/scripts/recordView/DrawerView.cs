using Godot;
using System.Collections.Generic;

namespace Musikspieler.Scripts.RecordView
{
    public partial class DrawerView : ScrollView<IPlaylist>
    {
        public override void _Ready()
        {
            base._Ready();

            //Animationfunctions setzen
            Animation = new Animations(
                forwardMargin: 0.5f,
                backwardMargin: 0.5f,
                Animations.SelectedOffsetXAnimationFucntion
            );

            itemObjectWidth = 5f;
            scrollAreaSize = 0.3f;
            flipThresholdOffset = 0f;
            flipThreshold = 0f;
            mask = Scripts.RecordView.CollisionMask.DrawerViewBoundary;

            //NUR FÜR TESTZWECKE
            GD.Print("DrawerView created");
            List<IPlaylist> playlists = new(15);
            for (int i = 0; i < 15; i++)
            {
                List<ISong> songs = new(100);
                for (int s = 0; s < 100; s++)
                {
                    songs.Add(new Song(Utility.RandomString(10)));
                }
                playlists.Add(new Playlist(songs, $"Playlist {i}"));
            }
            MusicCollection dir = new();
            ItemList = dir;
            dir.AddItems(playlists);
        }
    }
}
