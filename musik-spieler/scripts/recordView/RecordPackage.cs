using Godot;
using System;

namespace Musikspieler.Scripts
{
    public partial class RecordPackage : SmoothMovingObject
    {
        [Export] private MeshInstance3D _meshInstance;

        //private RecordGrabHandler _grabHandler;

        public static readonly ShaderMaterial defaultMaterial;

        private static readonly PackedScene recordPackagePrefab = GD.Load<PackedScene>("res://scenes/recordView/recordPackage.tscn");

        /// <summary>
        /// Ob die Packung sich gerade außerhalb des Views bewegt. Immer true, wenn isGettingDragged true ist, aber auch, wenn noch Animationen abgespielt werden nach dem Loslassen.
        /// </summary>
        public bool IsPending { get; private set; }

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
                {
                    IsPending = true;
                    _meshInstance.MaterialOverride = defaultMaterial;
                    SmoothReparent((Node3D)GetViewport().GetChild(0));
                }
                else
                {
                    SmoothReparent(RecordView.RecordsContainer);
                }
            }
        }

        /// <summary>
        /// Welches Lied diese Packung repräsentiert.
        /// </summary>
        public ISong song;

        /// <summary>
        /// An welchem Index diese Packung gerade in seinem View liegt. Wenn die Packung herumgezogen wird, zeigt der Index immer noch auf die Stelle, wo es herkam.
        /// </summary>
        public int ViewIndex { get; private set; }

        private RecordView _view;
        public RecordView RecordView
        {
            get => _view;
            private set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (_view != null)
                    _view.PlaylistChanged -= OnPlaylistChanged;
                _view = value;
                _view.PlaylistChanged += OnPlaylistChanged;
            }
        }

        private void OnPlaylistChanged(RecordView.PlaylistChangedEventArgs args)
        {
            if (args.RecordsRemoved && args.packages.Contains(this))
                RecordView = args.changeToView;

            ViewIndex = RecordView.IndexOf(this);
            if (ViewIndex == -1)
                throw new Exception($"Einer {nameof(RecordPackage)} ist einem {nameof(RecordView)} zugewiesen, der sie nicht enthält.");
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (IsPending && !IsGettingDragged && IsCloseToTargetPosition)
            {
                _meshInstance.MaterialOverride = RecordView.CutoffMaterialInstance;
            }
        }

        //Ausblenden ist sinnvoll hier, da, wenn ein Objekt alleine andere Parameter bekommt, die Parameter nicht mehr synchron angepasst werden, was der Sinn der statischen Variable ist.
        public static readonly new SmoothDamp SmoothDamp;

        static RecordPackage()
        {
            //statischer Konstruktor, um die Konstanten zu benennen, und kein "Magic Numbers" zu übergeben, und sie trotzdem nicht in der Klasse herumfliegen zu haben.
            const float PositionSmoothTime = 0.10f;
            const float PositionMaxSpeed = 20f;
            const float RotationSmoothTime = 0.07f;
            const float RotationMaxSpeed = 40f;
            const float ScaleSmoothTime = 0.10f;
            const float ScaleMaxSpeed = 20f;

            SmoothDamp = new(PositionSmoothTime, PositionMaxSpeed, RotationSmoothTime, RotationMaxSpeed, ScaleSmoothTime, ScaleMaxSpeed);

            //load default material
            defaultMaterial = GD.Load<ShaderMaterial>("res://graphics/defaultRecordPackageMaterial.tres");
        }

        ///Im Gegensatz zu Unity kann in Godot mit Konstruktoren gearbeitet werden. Argumente sind dennoch nicht möglich, da der Konstruktor außerhalb unseres Codes aufgerufen wird.
        ///Deshalb wird hier mit dem Factory-Prinzip gearbeitet.
        private RecordPackage()
        {
            base.SmoothDamp = SmoothDamp;
        }

        public static RecordPackage InstantiateAndAssign(RecordView recordView, int playlistIndex)
        {
            ISong song = recordView.Playlist[playlistIndex];
            var package = (RecordPackage)recordPackagePrefab.Instantiate();
            package.song = song;
            package.RecordView = recordView;
            package._meshInstance.MaterialOverride = recordView.CutoffMaterialInstance;
            return package;
        }
    }
}
