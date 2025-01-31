namespace VirtualMeshCreator.Rendering
{
    public readonly struct VisibilityBuffer
    {
        public int[,] ObjectIDs { get; }
        public float[,] Depths { get; }

        public VisibilityBuffer(int width, int height)
        {
            ObjectIDs = new int[width, height];
            Depths = new float[width, height];

            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {
                    ObjectIDs[x, y] = -1;
                    Depths[x, y] = float.MaxValue;
                }
            }
        }

        public void SetVisibility(int x, int y, int objectId, float depth)
        {
            if(depth < Depths[x, y])
            {
                ObjectIDs[x, y] = objectId;
                Depths[x, y] = depth;
            }
        }

        public bool IsVisible(int x, int y, float depth)
        {
            return depth < Depths[x, y];
        }
    }
}
