using System.Runtime.InteropServices;
using VirtualMeshCreator.Math.Internal;

namespace VirtualMeshCreator.DataStructures.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FrustumCullData
    {
        public float3 RectMin;
        public float3 RectMax;

        public bool bCrossesFarPlane;
        public bool bCrossesNearPlane;
        public bool bFrustumSideCulled;
        public bool bIsVisible;
    }
}
