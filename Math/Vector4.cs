namespace VirtualMeshCreator.Math
{
    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public float magnitude => (float)System.Math.Sqrt((double)(x * x + y * y + z * z + w * w));
        public float magnitudeSqr => x * x + y * y + z * z + w * w;
        public Vector4 normalized => new Vector4(x / magnitude, y / magnitude, z / magnitude, w / magnitude);

        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public void Normalize()
        {
            x /= magnitude;
            y /= magnitude;
            z /= magnitude;
            w /= magnitude;
        }
    }
}
