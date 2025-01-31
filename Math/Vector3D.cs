namespace VirtualMeshCreator.Math
{
    public struct Vector3D
    {
        public double x;
        public double y;
        public double z;

        public double magnitude => System.Math.Sqrt(x * x + y * y + z * z);
        public Vector3D normalized => new Vector3D(x / magnitude, y / magnitude, z / magnitude);

        public static Vector3D zero = new Vector3D(0f, 0f, 0f);
        public static Vector3D one = new Vector3D(1f, 1f, 1f);
        public static Vector3D up = new Vector3D(0f, 1f, 0f);
        public static Vector3D forward = new Vector3D(0f, 0f, 1f);
        public static Vector3D right = new Vector3D(1f, 0f, 0f);

        public Vector3D(double x, double y, double z)
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

        public static double Dot(Vector3D v1, Vector3D v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public static Vector3D Cross(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);
        }

        public static Vector3D operator +(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vector3D operator +(Vector3D v1, double v)
        {
            return new Vector3D(v1.x + v, v1.y + v, v1.z + v);
        }

        public static Vector3D operator -(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static Vector3D operator -(Vector3D v1, double v)
        {
            return new Vector3D(v1.x - v, v1.y - v, v1.z - v);
        }

        public static Vector3D operator *(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }

        public static Vector3D operator *(Vector3D v1, double v)
        {
            return new Vector3D(v1.x * v, v1.y * v, v1.z * v);
        }

        public static bool operator <(Vector3D v1, Vector3D v2)
        {
            return v1.x < v2.x || v1.y < v2.y || v1.z < v2.z;
        }

        public static bool operator >(Vector3D v1, Vector3D v2)
        {
            return v1.x > v2.x || v1.y > v2.y || v1.z > v2.z;
        }
    }
}
