using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeNet
{
    public struct RoomID : IEquatable<RoomID>
    {
        public static readonly RoomID Empty = new RoomID(0);

        public class EqualityComparer : IEqualityComparer<RoomID>
        {
            public static EqualityComparer Instance { get; private set; } = new EqualityComparer();

            public bool Equals(RoomID x, RoomID y)
            {
                return x == y;
            }

            public int GetHashCode(RoomID obj)
            {
                return obj.GetHashCode();
            }
        }

        private static long Value = 0;


        public static RoomID Generate()
        {
            long value = 0;
            do
            {
                value = Interlocked.Increment(ref Value);
            } while (value == Empty.ID);

            return new RoomID(value);
        }

        public long ID { get; private set; }

        public RoomID(long id)
        {
            ID = id;
        }

        public static bool operator ==(RoomID lhs, RoomID rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(RoomID lhs, RoomID rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(RoomID other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RoomID))
            {
                return false;
            }
            var other = (RoomID)obj;
            return other.ID == ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override string ToString()
        {
            return ID.ToString();
        }

    }
}
