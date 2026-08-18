using System;
using UnityEngine;

namespace LWS.InterstateHauler
{
    [Serializable]
    public struct LwsWorldPositionD : IEquatable<LwsWorldPositionD>
    {
        public double x;
        public double y;
        public double z;

        public LwsWorldPositionD(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static LwsWorldPositionD Zero => new LwsWorldPositionD(0d, 0d, 0d);

        public double HorizontalMagnitude => Math.Sqrt(x * x + z * z);
        public double SqrMagnitude => x * x + y * y + z * z;

        public static LwsWorldPositionD FromVector3(Vector3 value)
        {
            return new LwsWorldPositionD(value.x, value.y, value.z);
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float)x, (float)y, (float)z);
        }

        public Vector3 ToLocalVector3(LwsWorldPositionD originOffset)
        {
            return (this - originOffset).ToVector3();
        }

        public double DistanceTo(LwsWorldPositionD other)
        {
            double dx = x - other.x;
            double dy = y - other.y;
            double dz = z - other.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public double HorizontalDistanceTo(LwsWorldPositionD other)
        {
            double dx = x - other.x;
            double dz = z - other.z;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        public bool ApproximatelyEquals(LwsWorldPositionD other, double toleranceMeters = 0.001d)
        {
            return DistanceTo(other) <= Math.Max(0d, toleranceMeters);
        }

        public bool Equals(LwsWorldPositionD other)
        {
            return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
        }

        public override bool Equals(object obj)
        {
            return obj is LwsWorldPositionD other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x.GetHashCode();
                hash = hash * 31 + y.GetHashCode();
                hash = hash * 31 + z.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({x:0.###}, {y:0.###}, {z:0.###})";
        }

        public static LwsWorldPositionD operator +(LwsWorldPositionD a, LwsWorldPositionD b)
        {
            return new LwsWorldPositionD(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static LwsWorldPositionD operator -(LwsWorldPositionD a, LwsWorldPositionD b)
        {
            return new LwsWorldPositionD(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static LwsWorldPositionD operator -(LwsWorldPositionD value)
        {
            return new LwsWorldPositionD(-value.x, -value.y, -value.z);
        }

        public static bool operator ==(LwsWorldPositionD a, LwsWorldPositionD b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(LwsWorldPositionD a, LwsWorldPositionD b)
        {
            return !a.Equals(b);
        }
    }
}
