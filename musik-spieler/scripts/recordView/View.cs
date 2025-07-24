using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musikspieler.Scripts.RecordView
{
    public abstract partial class View : StaticBody3D
    {
        public abstract ViewItem GrabItem(bool allowGrabChildren);
        public abstract bool MoveItem(int index, View targetView);
        public abstract bool AcceptItem(ViewItem item, int? index);
        public abstract bool IsInitialized { get; }
        public abstract CollisionShape3D BoundsShape { get; }
        public ShaderMaterial LocalMaterial { get; private set; }
        public abstract int GetViewIndex(ViewItem item);
        public abstract ViewItem this[int index] { get; }
        public abstract int ItemCount { get; }

        public abstract event Action<ItemListChangedEventArgs> ObjectsChanged;

        public struct ItemListChangedEventArgs
        {
            public readonly bool ViewChanged => changeToView != null;

            public List<ViewItem> items;
            public View changeToView;
        }

        public View()
        {
            if (!TypeCompatibilites.ContainsKey(GetType()))
            {
                RegisterViewType(GetType());
            }

            LocalMaterial = (ShaderMaterial)ViewItem.DefaultMaterial.Duplicate();
        }

        internal static void RegisterViewType(Type viewType)
        {
            if (TypeCompatibilites.ContainsKey(viewType))
                return;

            List<Type> implementsTypes = [];

            var implementedInterfaces = viewType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAcceptsItemType<>));

            foreach (var @interface in implementedInterfaces)
            {
                var args = @interface.GetGenericArguments();
                Console.WriteLine("Typargument(e): " + string.Join(", ", args.Select(a => a.Name)));
                implementsTypes.AddRange(args);
            }

            TypeCompatibilites.Add(viewType, implementsTypes);
        }

        public bool IsCompatibleWith(ViewItem item)
        {
            return IsCompatibleWith(item.GetType());
        }

        public bool IsCompatibleWith(Type itemType)
        {
            return AreCompatible(itemType, GetType());
        }

        public static bool AreCompatible(Type itemType, Type viewType)
        {
            List<Type> viewAcceptsTypes, itemImplementsTypes;
            while (!View.TypeCompatibilites.TryGetValue(viewType, out viewAcceptsTypes))
                View.RegisterViewType(viewType);
            while (!ViewItem.TypeCompatibilites.TryGetValue(itemType, out itemImplementsTypes))
                ViewItem.RegisterItemType(itemType);
            return itemImplementsTypes.Any(viewAcceptsTypes.Contains);
        }

        public static Dictionary<Type, List<Type>> TypeCompatibilites { get; private set; } = [];

        // nodes can request to get their transform targets set
        public abstract void UpdateItemTransform(int index);

        // a node that the items can parent to
        public abstract ScrollViewContentContainer Container { get; }

        protected Mask<CollisionMask> mask;

        public bool IsUnderCursor
        {
            get => RaycastHandler.IsObjectUnderCursor(this);
        }
    }
}
