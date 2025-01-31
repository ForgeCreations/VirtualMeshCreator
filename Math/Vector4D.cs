namespace VirtualMeshCreator.Math
{
    public struct Vector4D
    {
        public double x;
        public double y;
        public double z;
        public double w;

        public double magnitude => System.Math.Sqrt(x * x + y * y + z * z + w * w);

        public Vector4D(double x, double y, double z, double w)
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
