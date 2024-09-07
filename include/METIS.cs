using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace VirtualMeshCreator
{
    public static class METIS
    {
        public static int PartGraphRecursive(ref int nvtxs, ref int ncon, int[] xadj, int[] adjncy, int[] vwgt, int[] vsize, int[] adjwgt, ref int tparts, float[] tpwgts, float[] ubvec, int[] options, ref int edgecut, out int[] part)
        {
            part = new int[nvtxs];
            return METIS_PartGraphRecursive(ref nvtxs, ref ncon, xadj, adjncy, vwgt, vsize, adjwgt, ref tparts, tpwgts, ubvec, options, ref edgecut, part);
        }

        [DllImport("MetisDLL.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl), MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int METIS_PartGraphRecursive(ref int nvtxs, ref int ncon, int[] xadj, int[] adjncy, int[] vwgt, int[] vsize, int[] adjwgt, ref int tparts, float[] tpwgts, float[] ubvec, int[] options, ref int edgecut, [Out] int[] part);

        public const int METIS_OK = 1;
    }
}
