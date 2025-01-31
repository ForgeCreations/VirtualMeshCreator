namespace VirtualMeshCreator.Math
{
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public float magnitude => (float)System.Math.Sqrt(x * x + y * y + z * z);
        public float magnitudeSqr => x * x + y * y + z * z;
        public Vector3 normalized => new Vector3(x / magnitude, y / magnitude, z / magnitude);

        public static Vector3 zero = new Vector3(0f, 0f, 0f);
        public static Vector3 one = new Vector3(1f, 1f, 1f);
        public static Vector3 up = new Vector3(0f, 1f, 0f);
        public static Vector3 forward = new Vector3(0f, 0f, 1f);
        public static Vector3 right = new Vector3(1f, 0f, 0f);

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return x;
                    case 1:
                        return y;
                    case 2:
                        return z;
                    default:
                        return float.NaN;
                }
            }

            set
            {
                if (index == 0)
                    x = value;
                else if (index == 1)
                    y = value;
                else if (index == 2)
                    z = value;
            }
        }

        public Vector3(float x, float y, float z)
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

        public static float Dot(Vector3 v1, Vector3 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public static Vector3 Cross(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
        }

        public static Vector3 Abs(Vector3 vec)
        {
            return new Vector3(System.Math.Abs(vec.x), System.Math.Abs(vec.y), System.Math.Abs(vec.z));
        }

        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vector3 operator +(Vector3 v1, float v)
        {
            return new Vector3(v1.x + v, v1.y + v, v1.z + v);
        }

        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static Vector3 operator -(Vector3 v1, float v)
        {
            return new Vector3(v1.x - v, v1.y - v, v1.z - v);
        }

        public static Vector3 operator *(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }

        public static Vector3 operator *(Vector3 v1, float v)
        {
            return new Vector3(v1.x * v, v1.y * v, v1.z * v);
        }

        public static Vector3 operator *(float v, Vector3 v1)
        {
            return new Vector3(v1.x * v, v1.y * v, v1.z * v);
        }

        public static Vector3 operator /(Vector3 v1, float v)
        {
            return new Vector3(v1.x / v, v1.y / v, v1.z / v);
        }

        public static bool operator <(Vector3 v1, Vector3 v2)
        {
            return v1.x < v2.x || v1.y < v2.y || v1.z < v2.z;
        }

        public static bool operator >(Vector3 v1, Vector3 v2)
        {
            return v1.x > v2.x || v1.y > v2.y || v1.z > v2.z;
        }

        public static bool operator ==(Vector3 v1, Vector3 v2)
        {
            return v1.x == v2.x || v1.y == v2.y || v1.z == v2.z;
        }

        public static bool operator !=(Vector3 v1, Vector3 v2)
        {
            return v1.x != v2.x || v1.y != v2.y || v1.z != v2.z;
        }

        public static Vector3 operator |(Vector3 v1, Vector3 v2)
        {
            return new Vector3((long)v1.x | (long)v2.x, (long)v1.y ^ (long)v2.y, (long)v1.z ^ (long)v2.z);
        }

        public static Vector3 operator ^(Vector3 v1, Vector3 v2)
        {
            return new Vector3((uint)v1.x ^ (int)v2.x, (uint)v1.y ^ (int)v2.y, (uint)v1.z ^ (int)v2.z);
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
