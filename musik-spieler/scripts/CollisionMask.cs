using Godot;
using System;
using System.Linq;

namespace Musikspieler.Scripts
{
    public enum CollisionMask
    {
        Default = (0 << 1),
        RecordViewBoundary = (1 << 1),
        DrawerViewBoundary = (2 << 1),
        GlobalDragPlane = (3 << 1),
    }

    public struct Mask<T> where T : Enum
    {
        uint mask;

        public Mask(uint mask)
        {
            this.mask = mask;
        }

        public Mask(T type)
        {
            mask = Convert.ToUInt32(type);
        }

        public readonly bool Contains(T type)
        {
            return (Convert.ToUInt32(type) & mask) > 0;
        }

        public readonly bool ContainsAny(Mask<T> type)
        {
            return (type.mask & mask) > 0;
        }

        public readonly bool ContainsAll(Mask<T> type)
        {
            return (type.mask | mask) == mask;
        }

        public static Mask<T> GetMask(T type1, T type2)
        {
            return new(Convert.ToUInt32(type1) | Convert.ToUInt32(type2));
        }

        public static Mask<T> GetMask(params T[] types)
        {
            if (types == null || types.Length == 0) throw new ArgumentNullException(nameof(types));
            Mask<T> mask = new(Convert.ToUInt32(types[0]));
            for (int i = 1; i < types.Length; i++)
            {
                mask.mask |= Convert.ToUInt32(types[i]);
            }
            return mask;
        }

        public void Remove(T type)
        {
            mask &= ~Convert.ToUInt32(type);
        }

        public void Add(T type)
        {
            mask |= Convert.ToUInt32(type);
        }

        public static implicit operator Mask<T>(T bitField)
        {
            return new Mask<T>(bitField);
        }

        public static implicit operator uint(Mask<T> mask)
        {
            return mask.mask;
        }
    }
}
