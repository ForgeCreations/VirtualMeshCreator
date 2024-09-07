using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualMeshCreator.Rendering
{
    public class VisibilityBuffer
    {
        /// <summary>
        /// Data = 32 bits: Depth, 17 bits: Visible Cluster ID, 8 bits: Triangle ID
        /// </summary>
        public ulong[,] Data;

        public VisibilityBuffer(int width, int height)
        {
            Data = new ulong[width, height];
        }
    }
}
