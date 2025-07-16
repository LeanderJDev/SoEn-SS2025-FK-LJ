using Godot;
using System;

namespace Musikspieler.Scripts
{
    public partial class RecordPackage : SmoothMovingObject
    {
        [Export] private MeshInstance3D _meshInstance;

        public static readonly ShaderMaterial defaultMaterial;

        /// <summary>
        /// Ob die Packung sich gerade auußerhalb des Views bewegt. Immer true, wenn isGettingDragged true ist, aber auch, wenn noch Animationen abgespielt werden nach dem Loslassen.
        /// </summary>
        public bool IsPending { get; }

        private bool _isGettingDragged;
        /// <summary>
        /// Ob die Packung gerade herumgezogen wird. Wird direkt auf false gesetzt, wenn der Nutzer loslässt.
        /// </summary>
        public bool IsGettingDragged
        {
            get => _isGettingDragged;
            set
            {
                _isGettingDragged = value;
                if (_isGettingDragged)
                    _meshInstance.MaterialOverride = defaultMaterial;
            }
        }

        /// <summary>
        /// Welches Lied diese Packung repräsentiert.
        /// </summary>
        public ISong song;

        /// <summary>
        /// An welchem Index diese Packung gerade in seinem View liegt. Wenn die Packung herumgezogen wird, zeigt der Index immernoch auf die Stelle, wo es herkam.
        /// </summary>
        public int ViewIndex { get; private set; }

        private ViewPlaylist _viewPlaylist;

        /// <summary>
        /// Note: Die aufgerufene RecordPackage muss in der Playlist sein, bevor die Playlist hiermit gesetzt werden kann!
        /// Wenn dieses Feld auf null gesetzt wird, wird die RecordPackage gelöscht.
        /// </summary>
        public ViewPlaylist Playlist
        {
            get => _viewPlaylist;
            set
            {
                if (_viewPlaylist != null)
                    _viewPlaylist.PlaylistChanged -= OnPlaylistChanged;
                _viewPlaylist = value;
                if (_viewPlaylist == null)
                {
                    GD.Print($"{nameof(RecordPackage)} des Songs {song.Name} wird gelöscht.");
                    QueueFree();
                    return;
                }
                ViewIndex = Playlist.IndexOf(this);
                if (ViewIndex == -1)
                    throw new Exception($"Die aufgerufene {nameof(RecordPackage)} muss in der {nameof(ViewPlaylist)} sein, bevor ihre Playlist-Eigenschaft gesetzt werden kann!");
                _viewPlaylist.PlaylistChanged += OnPlaylistChanged;
            }
        }

        private void OnPlaylistChanged()
        {
            ViewIndex = Playlist.IndexOf(this);
            _meshInstance.MaterialOverride = Playlist.recordView.CutoffMaterialInstance;
            //GD.Print($"Updated VieweIndex to: {ViewIndex}");
            if (ViewIndex == -1)
                throw new Exception($"Einer {nameof(RecordPackage)} ist eine {nameof(ViewPlaylist)} zugewiesen, die sie nicht enthält.");
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (IsPending && !IsGettingDragged && IsCloseToTargetPosition)
            {
                _meshInstance.MaterialOverride = Playlist.recordView.CutoffMaterialInstance;
            }
        }

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

            //load default material
            defaultMaterial = GD.Load<ShaderMaterial>("res://graphics/defaultRecordPackageMaterial.tres");
        }

        ///Im Gegensatz zu Unity kann in Godot mit Konstruktoren gearbeitet werden.
        public RecordPackage()
        {
            base.SmoothDamp = SmoothDamp;
        }
    }
}
