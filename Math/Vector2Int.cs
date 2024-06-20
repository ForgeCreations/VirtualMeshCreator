using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualMeshCreator.Math
{
    public struct Vector2Int
    {
        public int x;
        public int y;

        public int magnitude => (int)System.Math.Sqrt(x * x + y * y);
        public int magnitudeSqr => x * x + y * y;
        public Vector2Int normalized => new Vector2Int(x / magnitude, y / magnitude);

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2Int(int[] array)
        {
            this.x = array[0];
            this.y = array[1];
        }

        public void Normalize()
        {
            x /= magnitude;
            y /= magnitude;
        }

        public static int Dot(Vector2Int v1, Vector2Int v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

        public static Vector2Int operator +(Vector2Int v1, Vector2Int v2)
        {
            return new Vector2Int(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vector2Int operator +(Vector2Int v1, int v)
        {
            return new Vector2Int(v1.x + v, v1.y + v);
        }

        public static Vector2Int operator -(Vector2Int v1, Vector2Int v2)
        {
            return new Vector2Int(v1.x - v2.x, v1.y - v2.y);
        }

        public static Vector2Int operator -(Vector2Int v1, int v)
        {
            return new Vector2Int(v1.x - v, v1.y - v);
        }

        public static Vector2Int operator *(Vector2Int v1, Vector2Int v2)
        {
            return new Vector2Int(v1.x * v2.x, v1.y * v2.y);
        }

        public static Vector2Int operator *(Vector2Int v1, int v)
        {
            return new Vector2Int(v1.x * v, v1.y * v);
        }

        public static Vector2Int operator /(Vector2Int v1, Vector2Int v2)
        {
            return new Vector2Int(v1.x / v2.x, v1.y / v2.y);
        }

        public static Vector2Int operator /(Vector2Int v1, int v)
        {
            return new Vector2Int(v1.x / v, v1.y / v);
        }

        public static bool operator <(Vector2Int v1, Vector2Int v2)
        {
            return v1.x < v2.x || v1.y < v2.y;
        }

        public static bool operator >(Vector2Int v1, Vector2Int v2)
        {
            return v1.x > v2.x || v1.y > v2.y;
        }

        public static bool operator ==(Vector2Int v1, Vector2Int v2)
        {
            return v1.x == v2.x || v1.y == v2.y;
        }

        public static bool operator !=(Vector2Int v1, Vector2Int v2)
        {
            return v1.x != v2.x || v1.y != v2.y;
        }
    }
}
