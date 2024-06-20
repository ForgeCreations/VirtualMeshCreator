using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualMeshCreator.Math
{
    public struct DVector4
    {
        public double x;
        public double y;
        public double z;
        public double w;

        public double magnitude => System.Math.Sqrt(x * x + y * y + z * z + w * w);
        public double magnitudeSqr => x * x + y * y + z * z + w * w;
        public DVector4 normalized => new DVector4(x / magnitude, y / magnitude, z / magnitude, w / magnitude);

        public DVector4(double x, double y, double z, double w)
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
