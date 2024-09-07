using System.Runtime.InteropServices;

namespace VirtualMeshCreator.Math.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    public struct int4
    {
        public int x, y, z, w;

        public int4(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
}
