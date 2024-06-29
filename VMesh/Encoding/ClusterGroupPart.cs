using VirtualMeshCreator.Utility;

namespace VirtualMeshCreator.VMesh.Encoding
{
    /// <summary>
    /// Whole group or a part of a group that has been split.
    /// </summary>
    public struct ClusterGroupPart
    {
        /// <summary>
        /// Can be reordered during page allocation, so we need to store a list here.
        /// </summary>
        public int[] Clusters;
        public Bounds Bounds;
        public uint PageIndex;
        /// <summary>
        /// Index of group this is a part of.
        /// </summary>
        public uint GroupIndex;
        public uint HierarchyNodeIndex;
        public uint HierarchyChildIndex;
        public uint PageClusterOffset;
    }
}
