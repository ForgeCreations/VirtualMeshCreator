using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualMeshCreator.Math
{
    public struct Vector3Int
    {
        public int x;
        public int y;
        public int z;

        public int magnitude => (int)System.Math.Sqrt(x * x + y * y + z * z);
        public int magnitudeSqr => x * x + y * y + z * z;
        public Vector3Int normalized => new Vector3Int(x / magnitude, y / magnitude, z / magnitude);

        public static Vector3Int zero = new Vector3Int(0, 0, 0);
        public static Vector3Int one = new Vector3Int(1, 1, 1);
        public static Vector3Int up = new Vector3Int(0, 1, 0);
        public static Vector3Int forward = new Vector3Int(0, 0, 1);
        public static Vector3Int right = new Vector3Int(1, 0, 0);

        public Vector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Int(int[] array)
        {
            this.x = array[0];
            this.y = array[1];
            this.z = array[2];
        }

        public void Normalize()
        {
            x /= magnitude;
            y /= magnitude;
            z /= magnitude;
        }

        public static int Dot(Vector3Int v1, Vector3Int v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public static Vector3Int Cross(Vector3Int v1, Vector3Int v2)
        {
            return new Vector3Int(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
        }

        public static Vector3Int operator +(Vector3Int v1, Vector3Int v2)
        {
            return new Vector3Int(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vector3Int operator +(Vector3Int v1, int v)
        {
            return new Vector3Int(v1.x + v, v1.y + v, v1.z + v);
        }

        public static Vector3Int operator -(Vector3Int v1, Vector3Int v2)
        {
            return new Vector3Int(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static Vector3Int operator -(Vector3Int v1, int v)
        {
            return new Vector3Int(v1.x - v, v1.y - v, v1.z - v);
        }

        public static Vector3Int operator *(Vector3Int v1, Vector3Int v2)
        {
            return new Vector3Int(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }

        public static Vector3Int operator *(Vector3Int v1, int v)
        {
            return new Vector3Int(v1.x * v, v1.y * v, v1.z * v);
        }

        public static Vector3Int operator /(Vector3Int v1, Vector3Int v2)
        {
            return new Vector3Int(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
        }

        public static Vector3Int operator /(Vector3Int v1, int v)
        {
            return new Vector3Int(v1.x / v, v1.y / v, v1.z / v);
        }

        public static bool operator <(Vector3Int v1, Vector3Int v2)
        {
            return v1.x < v2.x || v1.y < v2.y || v1.z < v2.z;
        }

        public static bool operator >(Vector3Int v1, Vector3Int v2)
        {
            return v1.x > v2.x || v1.y > v2.y || v1.z > v2.z;
        }

        public static bool operator ==(Vector3Int v1, Vector3Int v2)
        {
            return v1.x == v2.x || v1.y == v2.y || v1.z == v2.z;
        }

        public static bool operator !=(Vector3Int v1, Vector3Int v2)
        {
            return v1.x != v2.x || v1.y != v2.y || v1.z != v2.z;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
