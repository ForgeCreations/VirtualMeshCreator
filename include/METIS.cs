using System;
using System.Runtime.InteropServices;

namespace VirtualMeshCreator
{
    public static class METIS
    {
        public static int PartGraphRecursive(int nvtxs, int ncon, int[] xadj, int[] adjncy, int[] vwgt, int[] vsize, int[] adjwgt, int tparts, float[] tpwgts, float[] ubvec, int[] options, int edgecut, out int[] part)
        {
            part = new int[nvtxs];
            return METIS_PartGraphRecursive(ref nvtxs, ref ncon, xadj, adjncy, vwgt, vsize, adjwgt, ref tparts, tpwgts, ubvec, options, ref edgecut, out part);
        }

        [DllImport("MetisDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int METIS_PartGraphRecursive(ref int nvtxs, ref int ncon, int[] xadj, int[] adjncy, int[] vwgt, int[] vsize, int[] adjwgt, ref int tparts, float[] tpwgts, float[] ubvec, int[] options, ref int edgecut, out int[] part);

        public const int METIS_OK = 1;
    }
}
