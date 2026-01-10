using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeNet
{
    public struct UUID : IEquatable<UUID>
    {
        public class EqualityComparer : IEqualityComparer<UUID>
        {
            public static EqualityComparer Instance { get; private set; } = new EqualityComparer();

            public bool Equals(UUID x, UUID y)
            {
                return x == y;
            }

            public int GetHashCode(UUID obj)
            {
                return obj.GetHashCode();
            }
        }

        private static UUID Empty = new UUID(0);
        private static long Value = 0;


        public static UUID Generate()
        {
            long value = 0;
            do
            {
                value = Interlocked.Increment(ref Value);
            } while (value == Empty.ID);

            return new UUID(value);
        }

        public long ID { get; private set; }

        public UUID(long id)
        {
            ID = id;
        }

        public static bool operator ==(UUID lhs, UUID rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(UUID lhs, UUID rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(UUID other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UUID))
            {
                return false;
            }
            var other = (UUID)obj;
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
