using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualMeshCreator.Math
{
    public struct DVector3
    {
        public double x;
        public double y;
        public double z;

        public double magnitude => System.Math.Sqrt(x * x + y * y + z * z);
        public double magnitudeSqr => x * x + y * y + z * z;
        public DVector3 normalized => new DVector3(x / magnitude, y / magnitude, z / magnitude);

        public static DVector3 zero = new DVector3(0f, 0f, 0f);
        public static DVector3 one = new DVector3(1f, 1f, 1f);
        public static DVector3 up = new DVector3(0f, 1f, 0f);
        public static DVector3 forward = new DVector3(0f, 0f, 1f);
        public static DVector3 right = new DVector3(1f, 0f, 0f);

        public DVector3(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void Normalize()
        {
            x /= magnitude;
            y /= magnitude;
            z /= magnitude;
        }

        public static double Dot(DVector3 v1, DVector3 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public static DVector3 Cross(DVector3 v1, DVector3 v2)
        {
            return new DVector3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
        }

        public static DVector3 operator +(DVector3 v1, DVector3 v2)
        {
            return new DVector3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static DVector3 operator +(DVector3 v1, double v)
        {
            return new DVector3(v1.x + v, v1.y + v, v1.z + v);
        }

        public static DVector3 operator -(DVector3 v1, DVector3 v2)
        {
            return new DVector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static DVector3 operator -(DVector3 v1, double v)
        {
            return new DVector3(v1.x - v, v1.y - v, v1.z - v);
        }

        public static DVector3 operator *(DVector3 v1, DVector3 v2)
        {
            return new DVector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }

        public static DVector3 operator *(DVector3 v1, double v)
        {
            return new DVector3(v1.x * v, v1.y * v, v1.z * v);
        }

        public static bool operator <(DVector3 v1, DVector3 v2)
        {
            return v1.x < v2.x || v1.y < v2.y || v1.z < v2.z;
        }

        public static bool operator >(DVector3 v1, DVector3 v2)
        {
            return v1.x > v2.x || v1.y > v2.y || v1.z > v2.z;
        }
    }
}
