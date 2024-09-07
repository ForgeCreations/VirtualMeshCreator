using VirtualMeshCreator.VMesh.Encoding;

namespace VirtualMeshCreator.VMesh
{
    public struct VirtualMeshSettings
    {
        public PositionPrecision PositionPrecision;
        //public bool PreserveArea;
        public bool ExplicitTangents;
        public bool AnisotropicLODSelection;
        /// <summary>
        /// This is a greedy algorithm that in every step adds the triangle that would grab the largest possible area out of the remaining parts on the mesh.
        /// This is done during Simplification
        /// </summary>
        public bool MaxAreaTriangulation;
        public int NormalPrecision;
        public int TangentPrecision;
        public int TargetMinimumResidencyinKB;

        // Streaming
        public int StreamingPoolSize;

        // Extra
        public bool EnableTessellation;
        public bool AllowSkinnedMeshes;

        public static VirtualMeshSettings Default
        {
            get
            {
                VirtualMeshSettings settings = new VirtualMeshSettings
                {
                    PositionPrecision = PositionPrecision.StepSize4cm,
                    ExplicitTangents = false,
                    AnisotropicLODSelection = false,
                    MaxAreaTriangulation = false,
                    NormalPrecision = -1,
                    TangentPrecision = -1,
                    TargetMinimumResidencyinKB = 0,
                    StreamingPoolSize = 1024,
                    EnableTessellation = false,
                    AllowSkinnedMeshes = false
                };
                return settings;
            }
        }
    }
}
