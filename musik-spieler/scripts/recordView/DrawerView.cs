using Godot;
using Musikspieler.Scripts.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musikspieler.Scripts.RecordView
{
    public partial class DrawerView : ScrollView<IPlaylist>
    {
        private readonly List<bool> selected = [];

        public override void _Ready()
        {
            base._Ready();

            ItemsAdded += OnItemsAdded;
            ItemsRemoved += OnItemsRemoved;

            //Animationfunctions setzen
            Animation = new Animations(
                forwardMargin: 0.5f,
                backwardMargin: 0.5f,
                Animations.GapOffsetXAnimationFunction,
                SelectedOffsetAnimationFunction
            );

            itemObjectWidth = 3f;
            scrollAreaSize = 0.3f;
            flipThresholdOffset = 0f;
            flipThreshold = 0f;
            mask = Scripts.RecordView.CollisionMask.DrawerViewBoundary;
            autoScrollSensitivity = 20f;

            //NUR FÜR TESTZWECKE
            List<IPlaylist> playlists = new(15);
            for (int i = 0; i < 15; i++)
            {
                List<ISong> songs = new(100);
                for (int s = 0; s < 100; s++)
                {
                    songs.Add(new Song(Utility.RandomString(10), "Album", "Artist", 0, "Path"));
                }
                playlists.Add(new Playlist(songs, $"Playlist {i}"));
            }
            // PlaylistDirectory dir = new();
            IPlaylistDirectory dir = MusicCollection.Instance.PlaylistDirectory;
            ItemList = dir;
            dir.AddItems(playlists);
        }

        public override ViewItem GrabItem(bool allowGrabChildren)
        {
            if (selected == null || selected.Count <= 0)
                return null;

            if (selected[GapIndexClamped])
                return base.GrabItem(allowGrabChildren);

            return null;
        }

        private void OnItemsAdded(ItemsAddedEventArgs args)
        {
            selected.InsertRange(args.startIndex, Enumerable.Repeat(false, args.count));
        }

        private void OnItemsRemoved(ItemsRemovedEventArgs args)
        {
            selected.RemoveRange(args.startIndex, args.count);
        }

        public void SetSelected(int index, bool selected)
        {
            this.selected[index] = selected;
        }

        public AnimationOutput SelectedOffsetAnimationFunction(AnimationInput input)
        {
            const float selectedOffset = 2.4f;

            return new AnimationOutput()
            {
                PositionOffset = new(selected[input.index] ? selectedOffset : 0, 0, 0)
            };
        }
    }
}
