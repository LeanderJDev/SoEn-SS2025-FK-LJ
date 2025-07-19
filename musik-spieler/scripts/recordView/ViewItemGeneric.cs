using Godot;
using System;

namespace Musikspieler.Scripts.RecordView
{
    public abstract partial class ViewItem<T> : ViewItem where T : IItem
    {
        [Export] private MeshInstance3D _meshInstance;

        public static ViewItem<T> InstantiateAndAssign(ScrollView<T> recordView, int playlistIndex)
        {
            T song = recordView.Playlist[playlistIndex];
            var package = (ViewItem<T>)RecordPackagePrefab.Instantiate();
            package.song = song;
            package.View = recordView;
            package._meshInstance.MaterialOverride = recordView.CutoffMaterialInstance;
            return package;
        }

        protected static PackedScene RecordPackagePrefab { get; set; }

        /// <summary>
        /// Welches Lied diese Packung repräsentiert.
        /// </summary>
        public T song;

        private bool _isGettingDragged;
        /// <summary>
        /// Ob die Packung gerade herumgezogen wird. Wird direkt auf false gesetzt, wenn der Nutzer loslässt.
        /// </summary>
        public override bool IsGettingDragged
        {
            get => _isGettingDragged;
            set
            {
                _isGettingDragged = value;
                if (_isGettingDragged)
                {
                    IsPending = true;
                    _meshInstance.MaterialOverride = DefaultMaterial;
                    SmoothReparent((Node3D)GetViewport().GetChild(0));
                }
                else
                {
                    SmoothReparent(View.ScrollContainer);

                    //das hier muss schöner gehen eigentlich: jetzt sagt es einem anderen objekt, dass es bitte geupdated werden soll...
                    //Diese Fkt hier ist ja public, damit andere von außen evtl. refreshen können
                    View.UpdatePackageTransform(ViewIndex);
                }
            }
        }

        private ScrollView<T> _view;
        public ScrollView<T> View
        {
            get => _view;
            private set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (_view != null)
                    _view.PlaylistChanged -= OnPlaylistChanged;
                if (IsInsideTree() && IsGettingDragged)
                    SmoothReparent(value.ScrollContainer);
                _view = value;
                _view.PlaylistChanged += OnPlaylistChanged;
            }
        }

        public override bool Move(View targetView)
        {
            return View.MoveRecord(ViewIndex, targetView);
        }

        private void OnPlaylistChanged(ScrollView<T>.PlaylistChangedEventArgs args)
        {
            if (args.RecordsRemoved && args.packages.Contains(this))
                View = args.changeToView;

            ViewIndex = View.IndexOf(this);
            if (ViewIndex == -1)
                throw new Exception($"Einer {nameof(RecordPackage)} ist einem {nameof(Scripts.RecordView.View)} zugewiesen, der sie nicht enthält.");
        }

        public static SmoothDamp ObjectTypeSmoothDamp { get; protected set; }

        public static ShaderMaterial DefaultMaterial { get; protected set; }

        ///Im Gegensatz zu Unity kann in Godot mit Konstruktoren gearbeitet werden. Argumente sind dennoch nicht möglich, da der Konstruktor außerhalb unseres Codes aufgerufen wird.
        ///Deshalb wird hier mit dem Factory-Prinzip gearbeitet.
        protected ViewItem()
        {
            SmoothDamp = ObjectTypeSmoothDamp;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (IsPending && !IsGettingDragged && IsCloseToTargetPosition)
            {
                _meshInstance.MaterialOverride = View.CutoffMaterialInstance;
            }
        }

        static ViewItem()
        {
            //Muss ausgerufen werden, weil der statische Konstruktor von RecordPackage wortwörtlich zu faul ist.
            //Aber die Funktionalität direkt in die Init-Funktion zu schreiben würde bedeuten, dass man die Objekte erneut überschreiben kann, was Chaos erzeugen würde.
            //Und dann müsste man wieder neue Checks einbauen usw...
            RecordPackage.Init();
        }
    }
}
