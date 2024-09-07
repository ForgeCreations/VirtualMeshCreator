using System.Runtime.InteropServices;

namespace VirtualMeshCreator.Math.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    public struct float3
    {
        public float x, y, z;

        public float3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
