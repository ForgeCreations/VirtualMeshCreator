using System.Runtime.InteropServices;

namespace VirtualMeshCreator.Math.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    public struct int3
    {
        public int x, y, z;

        public int3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
