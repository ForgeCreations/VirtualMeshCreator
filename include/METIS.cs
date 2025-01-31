using System.Runtime.InteropServices;

namespace VirtualMeshCreator
{
    public static class METIS
    {
        private const string METIS_DLL = "Metis.dll";

        // METIS return codes
        public const int METIS_OK = 1;
        public const int METIS_ERROR_INPUT = -2;
        public const int METIS_ERROR_MEMORY = -3;
        public const int METIS_ERROR = -4;

        // METIS option array size
        public const int METIS_NOPTIONS = 40;

        // METIS option keys
        public const int METIS_OPTION_PTYPE = 0;    // Partitioning type
        public const int METIS_OPTION_OBJTYPE = 1;  // Objective type
        public const int METIS_OPTION_CTYPE = 2;    // Coarsening scheme
        public const int METIS_OPTION_IPTYPE = 3;   // Initial partitioning
        public const int METIS_OPTION_RTYPE = 4;    // Refinement Type

        [DllImport(METIS_DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int METIS_PartGraphRecursive(
            ref int nvtxs,
            ref int ncon,
            int[] xadj,
            int[] adjncy,
            int[] vwgt,
            int[] vsize,
            int[] adjwgt,
            ref int nparts,
            float[] tpwgts,
            float[] ubvec,
            int[] options,
            ref int edgeCut,
            int[] part
        );

        public static int PartGraphRecursive(
            int nvtxs,
            int ncon,
            int[] xadj,
            int[] adjncy,
            int[] vwgt,
            int[] vsize,
            int[] adjwgt,
            int nparts,
            float[] tpwgts,
            float[] ubvec,
            int[] options,
            int edgeCut,
            int[] part
        )
        {
            return METIS_PartGraphRecursive(ref nvtxs, ref ncon, xadj, adjncy, vwgt, vsize, adjwgt, ref nparts, tpwgts, ubvec, options, ref edgeCut, part);
        }
    }
}
