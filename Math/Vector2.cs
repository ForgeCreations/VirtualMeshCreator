using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualMeshCreator.Math
{
    public struct Vector2
    {
        public float x;
        public float y;

        public float magnitude => (float)System.Math.Sqrt(x * x + y * y);
        public float magnitudeSqr => x * x + y * y;
        public Vector2 normalized => new Vector2(x / magnitude, y / magnitude);

        public static Vector2 zero = new Vector2(0f, 0f);
        public static Vector2 one = new Vector2(1f, 1f);
        public static Vector2 up = new Vector2(0f, 1f);
        public static Vector2 forward = new Vector2(0f, 0f);
        public static Vector2 right = new Vector2(1f, 0f);

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public void Normalize()
        {
            x /= magnitude;
            y /= magnitude;
        }

        public static float Dot(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vector2 operator +(Vector2 v1, float v)
        {
            return new Vector2(v1.x + v, v1.y + v);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x - v2.x, v1.y - v2.y);
        }

        public static Vector2 operator -(Vector2 v1, float v)
        {
            return new Vector2(v1.x - v, v1.y - v);
        }

        public static Vector2 operator *(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x * v2.x, v1.y * v2.y);
        }

        public static Vector2 operator *(Vector2 v1, float v)
        {
            return new Vector2(v1.x * v, v1.y * v);
        }

        public static bool operator <(Vector2 v1, Vector2 v2)
        {
            return v1.x < v2.x || v1.y < v2.y;
        }

        public static bool operator >(Vector2 v1, Vector2 v2)
        {
            return v1.x > v2.x || v1.y > v2.y;
        }

        public static bool operator ==(Vector2 v1, Vector2 v2)
        {
            return v1.x == v2.x || v1.y == v2.y;
        }

        public static bool operator !=(Vector2 v1, Vector2 v2)
        {
            return v1.x != v2.x || v1.y != v2.y;
        }
    }
}
