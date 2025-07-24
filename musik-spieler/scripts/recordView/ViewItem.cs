using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musikspieler.Scripts.RecordView
{
    //als Zwischenlayer, damit der GrabHandler ViewItems jeglichen Typs anfassen kann
    public abstract partial class ViewItem : SmoothMovingObject
    {
        public ViewItem()
        {
            GD.Print($"itemType constructor: {GetType()}");

            if (!TypeCompatibilites.ContainsKey(GetType()))
            {
                RegisterItemType(GetType());
            }

            ///Im Gegensatz zu Unity kann in Godot mit Konstruktoren gearbeitet werden. Argumente sind dennoch nicht möglich, da der Konstruktor außerhalb unseres Codes aufgerufen wird.
            ///Deshalb wird hier mit dem Factory-Prinzip gearbeitet.
            SmoothDamp = ObjectTypeSmoothDamp;
        }

        internal static void RegisterItemType(Type itemType)
        {
            if (TypeCompatibilites.ContainsKey(itemType))
                return;

            List<Type> implementsTypes = [];

            var implementedInterfaces = itemType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IItemType<>));

            foreach (var @interface in implementedInterfaces)
            {
                var args = @interface.GetGenericArguments();
                Console.WriteLine("Typargument(e): " + string.Join(", ", args.Select(a => a.Name)));
                implementsTypes.AddRange(args);
            }

            TypeCompatibilites.Add(itemType, implementsTypes);
        }

        public bool IsCompatibleWith(View view)
        {
            return IsCompatibleWith(view.GetType());
        }

        public bool IsCompatibleWith(Type viewType)
        {
            return AreCompatible(GetType(), viewType);
        }

        public static bool AreCompatible(Type itemType, Type viewType)
        {
            if (!typeof(ViewItem).IsAssignableFrom(itemType))
                return false;

            if (!typeof(View).IsAssignableFrom(viewType))
                return false;

            GD.Print("compatible?");
            GD.Print(itemType);
            GD.Print(viewType);

            List<Type> viewAcceptsTypes, itemImplementsTypes;
            while (!View.TypeCompatibilites.TryGetValue(viewType, out viewAcceptsTypes))
                View.RegisterViewType(viewType);
            while (!ViewItem.TypeCompatibilites.TryGetValue(itemType, out itemImplementsTypes))
                ViewItem.RegisterItemType(itemType);
            return itemImplementsTypes.Any(viewAcceptsTypes.Contains);
        }

        public static Dictionary<Type, List<Type>> TypeCompatibilites { get; private set; } = [];


        /// <summary>
        /// An welchem Index diese Packung gerade in seinem ChildView liegt. Wenn die Packung herumgezogen wird, zeigt der Index immer noch auf die Stelle, wo es herkam.
        /// </summary>
        public int ViewIndex { get; protected set; }

        /// <summary>
        /// Ob die Packung sich gerade ausserhalb des Views bewegt. Immer true, wenn isGettingDragged true ist, aber auch, wenn noch Animationen abgespielt werden nach dem Loslassen.
        /// </summary>
        public bool IsPending { get; protected set; }

        [Export] protected MeshInstance3D _meshInstance;

        protected PackedScene ItemPrefab { get; set; }

        /// <summary>
        /// Welches Lied diese Packung repraesentiert.
        /// </summary>
        public abstract IItem DisplayedItem { get; protected set; }

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
                    _meshInstance.MaterialOverride = DefaultMaterial;
                    if (GetViewport() == null)
                        GD.Print(GetType());
                    SmoothReparent((Node3D)GetViewport().GetChild(0));
                }
                else
                {
                    SmoothReparent(View.Container);

                    //das hier muss schöner gehen eigentlich: jetzt sagt es einem anderen objekt, dass es bitte geupdated werden soll...
                    //Diese Fkt hier ist ja public, damit andere von außen evtl. refreshen können
                    View.UpdateItemTransform(ViewIndex);
                }
            }
        }

        private View _view;
        public View View
        {
            get => _view;
            protected set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (_view != null)
                    _view.ObjectsChanged -= OnItemsChanged;
                if (IsInsideTree() && !IsGettingDragged)
                    SmoothReparent(value.Container);
                _view = value;
                _view.ObjectsChanged += OnItemsChanged;
            }
        }

        public bool Move(View targetView)
        {
            return View.MoveItem(ViewIndex, targetView);
        }

        private void OnItemsChanged(View.ItemListChangedEventArgs args)
        {
            if (args.ViewChanged && args.items.Contains(this))
                View = args.changeToView;

            ViewIndex = View.GetViewIndex(this);
            if (ViewIndex == -1)
            {
                GD.PrintErr($"Name: {Name}, ViewIndex: {ViewIndex}, ");
                throw new Exception($"Einer {this} von Typ {GetType()} ist einem {View.GetType()} ({View}) zugewiesen, der sie nicht enthält.");
            }
            View.UpdateItemTransform(ViewIndex);
        }

        public static SmoothDamp ObjectTypeSmoothDamp { get; protected set; }

        public static ShaderMaterial DefaultMaterial { get; protected set; }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (IsPending && !IsGettingDragged && IsCloseToTargetPosition)
            {
                _meshInstance.MaterialOverride = View.LocalMaterial;
            }
        }

        static ViewItem()
        {
            //Muss ausgerufen werden, weil der statische Konstruktor von RecordPackage wortwörtlich zu faul ist.
            //Aber die Funktionalität direkt in die Init-Funktion zu schreiben würde bedeuten, dass man die Objekte erneut überschreiben kann, was Chaos erzeugen würde.
            //Und dann müsste man wieder neue Checks einbauen usw...
            RecordPackage.Init();
            Drawer.Init();
        }
    }
}