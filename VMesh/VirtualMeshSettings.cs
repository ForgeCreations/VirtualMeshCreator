using VirtualMeshCreator.VMesh.Encoding;

namespace VirtualMeshCreator.VMesh
{
    public struct VirtualMeshSettings
    {
        public PositionPrecision PositionPrecision;

        // Streaming
        public int StreamingPoolSize;

        // Extra


        public static VirtualMeshSettings Default
        {
            get
            {
                VirtualMeshSettings settings = new VirtualMeshSettings();
                settings.PositionPrecision = PositionPrecision.StepSize4cm;

                settings.StreamingPoolSize = 1024;
                return settings;
            }
        }
    }
}
