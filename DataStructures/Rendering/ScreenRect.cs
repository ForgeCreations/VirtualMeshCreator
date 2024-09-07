using VirtualMeshCreator.Math.Internal;

namespace VirtualMeshCreator.DataStructures.Rendering
{
    public struct ScreenRect
    {
        public int4 Pixels;
        public bool bOverlapsPixelCenter;

        // For HZB sampling
        public int4 HZBTexels;
        public int HZBLevel;

        public float Depth;
    }
}
