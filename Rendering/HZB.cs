namespace VirtualMeshCreator.Rendering
{
    /// <summary>
    /// Heieraracal Z-Buffer
    /// </summary>
    public readonly struct HZB
    {
        private readonly float[,] zBuffer;
        private readonly int width, height;
        private readonly int levels;

        public HZB(int width, int height)
        {
            this.width = width;
            this.height = height;
            levels = (int)System.Math.Log(System.Math.Max(width, height), 2) + 1;
            zBuffer = new float[width, height];
            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {
                    zBuffer[x, y] = float.MaxValue;
                }
            }
        }

        public void UpdateZBuffer(int x, int y, float depth)
        {
            zBuffer[x, y] = System.Math.Min(zBuffer[x, y], depth);
            PropagateUp(x, y, depth);
        }

        private void PropagateUp(int x, int y, float depth)
        {
            for(int level = 1; level < levels; level++)
            {
                int step = 1 << level;
                int lx = x / step;
                int ly = y / step;
                if(lx < width / step && ly < height / step)
                {
                    zBuffer[lx, ly] = System.Math.Min(zBuffer[lx, ly], depth);
                }
            }
        }

        public bool IsOccluded(int x, int y, float depth)
        {
            for(int level = levels - 1; level >= 0; level--)
            {
                int step = 1 << level;
                int lx = x / step;
                int ly = y / step;
                if(lx < width / step && ly < height / step)
                {
                    if(depth >= zBuffer[lx, ly]) return true;
                }
            }
            return false;
        }
    }
}
