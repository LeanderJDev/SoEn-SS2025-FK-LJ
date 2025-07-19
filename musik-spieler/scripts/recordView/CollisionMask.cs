using System;

namespace Musikspieler.Scripts.RecordView
{
    public enum CollisionMask
    {
        Default = (1 << 0),
        RecordViewBoundary = (1 << 1),
        DrawerViewBoundary = (1 << 2),
        GlobalDragPlane = (1 << 3),
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

        public Mask(T type1, T type2)
        {
            mask = Convert.ToUInt32(type1) | Convert.ToUInt32(type2);
        }

        public Mask(params T[] types)
        {
            if (types == null || types.Length == 0) throw new ArgumentNullException(nameof(types));
            uint mask = Convert.ToUInt32(types[0]);
            for (int i = 1; i < types.Length; i++)
            {
                mask |= Convert.ToUInt32(types[i]);
            }
            this.mask |= mask;
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
