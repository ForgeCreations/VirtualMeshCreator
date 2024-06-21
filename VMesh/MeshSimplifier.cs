using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualMeshCreator.Math;
using VirtualMeshCreator.Utility;

namespace VirtualMeshCreator.VMesh
{
    public class MeshSimplifier
    {
        public class Quadric
        {
            public double a2, b2, c2, d2;
            public double ab, ac, ad;
            public double bc, bd, cd;

            public Quadric()
            {
                Array.Clear(new double[]
                {
                    a2, b2, c2, d2, ab, ac, ad, bc, bd, cd
                }, 0, 10);
            }

            public double this[int index] { get { return 0.0; } }

            public Quadric(DVector3 p0, DVector3 p1, DVector3 p2)
            {
                DVector3 n = DVector3.Cross(p1 - p0, p2 - p0).normalized;
                double a = n.x;
                double b = n.y;
                double c = n.z;
                double d = -DVector3.Dot(n, p0);
                a2 = a * a; b2 = b * b; c2 = c * c; d2 = d * d;
                ab = a * b; ac = a * c; ad = a * d;
                bc = b * c; bd = b * d; cd = c * d;
            }

            public void Add(Quadric b)
            {
                a2 += b.a2; b2 += b.b2; c2 += b.c2; d2 += b.d2;
                ab += b.ab; ac += b.ac; ad += b.ad;
                bc += b.bc; bd += b.bd; cd += b.cd;
            }

            public bool Get(Vector3 p)
            {
                DMatrix4x4 m = new DMatrix4x4(4), inv = new DMatrix4x4(4);
                m.SetColumn(0, new DVector4(a2, ab, ac, 0.0));
                m.SetColumn(1, new DVector4(ab, b2, bc, 0.0));
                m.SetColumn(2, new DVector4(ac, bc, c2, 0.0));
                m.SetColumn(3, new DVector4(ad, bd, cd, 1.0));
                if(!m.Invert(m, inv)) return false;
                DVector4 v = inv.column[3];
                p = new Vector3((float)v.x, (float)v.y, (float)v.z);
                return true;
            }

            public float Evaluate(Vector3 p)
            {
                float res = (float)(a2 * p.x * p.x + 2 * ab * p.x * p.y + 2 * ac * p.x * p.z + 2 * ad * p.x
                    + b2 * p.y * p.y + 2 * bc * p.y * p.z + 2 * bd * p.y
                    + c2 * p.z * p.z + 2 * cd * p.z + d2);
                return res <= 0.0f ? 0.0f : res;
            }
        }

        private int num_vert;
        private int num_index;
        private int num_tri;

        private readonly Vector3[] verts;
        private readonly int[] indexes;

        private HashTable vert_ht;
        private HashTable corner_ht;
        private int[] vert_refs;
        private int[] flags;
        private BitArray tri_removed;

        private enum Flag
        {
            AdjMask = 1,
            LockMask = 2
        };

        private List<(Vector3, Vector3)> edges;
        private HashTable edge0_ht;
        private HashTable edge1_ht;
        private Heap heap;

        private uint[] move_vert;
        private uint[] move_corner;
        private uint[] move_edge;
        private uint[] reevaluate_edge;

        private Quadric[] tri_quadrics;

        public int RemainingVertexCount { get; private set; } = 0;
        public int RemainingTriangleCount { get; private set; } = 0;
        public float MaxError { get; private set; } = 0;

        public MeshSimplifier(Vector3[] vertices, int vertexCount, int[] triangles, int triangleCount)
        {
            num_vert = vertexCount;
            num_index = triangleCount;
            num_tri = num_index / 3;
            verts = vertices;
            indexes = triangles;
            vert_ht = new HashTable((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(num_vert, 2.0)))));
            //vert_ht = new HashTable(num_vert);
            vert_refs = new int[num_vert];
            Array.Clear(vert_refs, 0, num_vert);
            corner_ht = new HashTable((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(num_index, 2.0)))));
            //corner_ht = new HashTable(num_index);
            tri_removed = new BitArray(num_tri);
            flags = new int[num_index];
            Array.Clear(flags, 0, num_index);
            RemainingVertexCount = num_vert;
            RemainingTriangleCount = num_tri;
            for(uint i = 0; i < num_vert; i++)
            {
                vert_ht.Add(MeshUtility.hash(verts[i]), i);
            }

            //Guess number of edges based on Euler's formula.
            uint NumEdges = MathUtil.Min3((uint)num_index, (uint)(3 * num_vert - 6), (uint)(num_tri + num_vert));
            Console.WriteLine("Num Edges: " + NumEdges);
            edges = new List<(Vector3, Vector3)>();
            edge0_ht = new HashTable(NumEdges);
            edge0_ht.Clear((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(NumEdges, 2.0)))), NumEdges);
            edge1_ht = new HashTable(NumEdges);
            edge1_ht.Clear((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(NumEdges, 2.0)))), NumEdges);

            for(uint corner = 0u; corner < num_index; corner++)
            {
                int vertIndex = indexes[corner];
                vert_refs[vertIndex]++;
                Vector3 p = verts[vertIndex];
                corner_ht.Add(MeshUtility.hash(p), corner);
                (Vector3, Vector3) vPair = (p, verts[indexes[MeshUtility.cycle3(corner)]]);
                //Console.WriteLine("Edge Count: " + edges.Length);
                if(AddEdgeht(vPair.Item1, vPair.Item2, (uint)edges.Count))
                {
                    edges.Add(vPair);
                }
            }
            Console.WriteLine("Done Initializing");
        }

        public bool AddEdgeht(Vector3 p0, Vector3 p1, uint index)
        {
            uint h0 = MeshUtility.hash(p0), h1 = MeshUtility.hash(p1);
            if(h0 > h1)
            {
                ArrayUtils.Swap(h0, h1);
                ArrayUtils.Swap(p0, p1);
            }

            uint OtherPairIndex;
            for(OtherPairIndex = edge0_ht.First(h0); edge0_ht.IsValid(OtherPairIndex); OtherPairIndex = edge0_ht.Next(OtherPairIndex))
            {
                //Console.WriteLine(index != OtherPairIndex);
                (Vector3, Vector3) OtherPair = edges[(int)OtherPairIndex];
                if(p0 == OtherPair.Item1 && p1 == OtherPair.Item2)
                    return false; // Found a duplicate
            }
            edge0_ht.Add(h0, index);
            edge1_ht.Add(h1, index);
            return true;
        }

        public void SetVertexIndex(uint corner, uint index)
        {
            uint v_idx = (uint)indexes[corner];
            //assert(v_idx != ~0u);
            Console.WriteLine(v_idx != ~0u);
            //assert(vert_refs[v_idx] > 0);
            Console.WriteLine(vert_refs[v_idx] > 0);

            if(v_idx == index) return;
            if(--vert_refs[v_idx] == 0)
            {
                vert_ht.Remove(MeshUtility.hash(verts[v_idx]), v_idx);
                RemainingVertexCount--;
            }
            v_idx = index;
            if(v_idx != ~0u) vert_refs[v_idx]++;
        }

        //Assign corner to the Keys.ToList()[0] encourntered identical point
        public void RemoveIfVertexDuplicate(uint corner)
        {
            int v_idx = indexes[corner];
            Vector3 v = verts[v_idx];
            uint v0 = MeshUtility.hash(v);
            for(uint i = vert_ht.First(v0); vert_ht.IsValid(i); vert_ht.Next(i))
            {
                if(i == v_idx) break;
                if(v == verts[i])
                {
                    SetVertexIndex(corner, i);
                    break;
                }
            }
        }

        public bool isTriangleDuplicate(int triangleIndex)
        {
            int i0 = indexes[triangleIndex * 3 + 0], i1 = indexes[triangleIndex * 3 + 1], i2 = indexes[triangleIndex * 3 + 2];
            uint v0 = MeshUtility.hash(verts[0]);
            for(uint i = corner_ht.First(v0); corner_ht.IsValid(i); corner_ht.Next(i))
            {
                if(i != triangleIndex * 3)
                {
                    if(i0 == indexes[i] && i1 == indexes[MeshUtility.cycle3(i)] && i2 == indexes[MeshUtility.cycle3(i, 2)])
                        return true;
                }
            }
            return false;
        }

        public void FixupTriangle(int tri_idx)
        {
            //assert(!tri_removed[tri_idx]);
            Console.WriteLine("Triangle Removed: " + !tri_removed[tri_idx]);

            Vector3 p0 = verts[indexes[tri_idx * 3 + 0]];
            Vector3 p1 = verts[indexes[tri_idx * 3 + 1]];
            Vector3 p2 = verts[indexes[tri_idx * 3 + 2]];

            bool is_removed = false;
            if(!is_removed)
            {
                is_removed = (p0 == p1) || (p1 == p2) || (p2 == p0);
            }

            if(!is_removed)
            {
                for(int k = 0; k < 3; k++) RemoveIfVertexDuplicate((uint)(tri_idx * 3 + k));
                is_removed = isTriangleDuplicate(tri_idx);
            }

            if(is_removed)
            {
                tri_removed.Set(tri_idx, true);
                RemainingTriangleCount--;
                for(int k = 0; k < 3; k++)
                {
                    uint corner = (uint)(tri_idx * 3 + k);
                    int v_idx = indexes[corner];
                    corner_ht.Remove(MeshUtility.hash(verts[v_idx]), corner);
                    SetVertexIndex(corner, ~0u);
                }
            }
            else tri_quadrics[tri_idx] = new Quadric(new DVector3(p0.x, p0.y, p0.z), new DVector3(p1.x, p1.y, p1.z), new DVector3(p2.x, p2.y, p2.z));
        }

        public void GatherAdjacentTriangles(Vector3 p, int[] triangles, out bool Lock)
        {
            Lock = false;
            uint p0 = MeshUtility.hash(p);
            for(uint i = corner_ht.First(p0); corner_ht.IsValid(i); corner_ht.Next(i))
            {
                if(verts[indexes[i]] == p)
                {
                    int tri_idx = (int)i / 3;
                    if((flags[tri_idx * 3] & (int)Flag.AdjMask) == 0)
                    {
                        flags[tri_idx * 3] |= (int)Flag.AdjMask;
                        triangles.ToList().Add(tri_idx);
                    }

                    if((flags[i] & (int)Flag.LockMask) == 1)
                    {
                        Lock = true;
                    }
                }
            }
        }

        public float Evaluate(Vector3 p0, Vector3 p1, bool merge)
        {
            if(p0 == p1) return 0.0f;

            float error = 0.0f;

            int[] adj_tris = new int[0];
            GatherAdjacentTriangles(p0, adj_tris, out bool lock0);
            GatherAdjacentTriangles(p1, adj_tris, out bool lock1);
            if(adj_tris.Length == 0) return 0.0f;
            if(adj_tris.Length > 24)
            {
                error += 0.5f * (adj_tris.Length - 24);
            }

            Quadric q = new Quadric();
            foreach(int i in adj_tris)
            {
                q.Add(tri_quadrics[i]);
            }
            Vector3 p = (p0 + p1) * 0.5f;

            bool is_valid_pos = (p - p0).magnitude + (p - p1).magnitude > 2 * (p0 - p1).magnitude;

            if(lock0 && lock1) error += 1e8f;
            if(lock0 && !lock1) p = p0;
            else if(!lock0 && lock1) p = p1;
            else if(!q.Get(p)) p = (p0 + p1) * 0.5f;
            if(!is_valid_pos)
            {
                p = (p0 + p1) * 0.5f;
            }
            error += q.Evaluate(p);

            if(merge)
            {
                Console.WriteLine("Merging Verticies");
                BeginMerge(p0); BeginMerge(p1);
                foreach(int i in adj_tris)
                {
                    for(int k = 0; k < 3; k++)
                    {
                        int corner = i * 3 + k;
                        Vector3 pos = verts[indexes[corner]];
                        if(pos == p0 || pos == p1)
                        {
                            pos = p;
                            if(lock0 || lock1) flags[corner] |= (int)Flag.LockMask;
                        }
                    }
                }

                foreach(int i in move_edge)
                {
                    (Vector3, Vector3) e = edges[i];
                    if(e.Item1 == p0 || e.Item1 == p1) e.Item1 = p;
                    if(e.Item2 == p0 || e.Item2 == p1) e.Item2 = p;
                }
                EndMerge();

                int[] adj_verts = new int[0];
                foreach(int i in adj_tris)
                {
                    for(int k = 0; k < 3; k++)
                    {
                        adj_verts.ToList().Add(indexes[i * 3 + k]);
                    }
                }
                Array.Sort(adj_verts, 0, adj_verts.Length);
                adj_verts.ToList().RemoveRange(0, adj_verts.Length);

                foreach(int v_idx in adj_verts)
                {
                    uint h = MeshUtility.hash(verts[v_idx]);
                    for(uint i = edge0_ht.First(h); edge0_ht.IsValid(i); edge0_ht.Next(i))
                    {
                        if(edges[(int)i].Item1 == verts[v_idx])
                        {
                            if(heap.IsPresent(i))
                            {
                                heap.Remove(i);
                                reevaluate_edge.ToList().Add(i);
                            }
                        }
                    }

                    for(uint i = edge1_ht.First(h); edge1_ht.IsValid(i); edge1_ht.Next(i))
                    {
                        if(edges[(int)i].Item2 == verts[v_idx])
                        {
                            if(heap.IsPresent(i))
                            {
                                heap.Remove(i);
                                reevaluate_edge.ToList().Add(i);
                            }
                        }
                    }
                }

                foreach(int i in adj_tris)
                {
                    FixupTriangle(i);
                }
            }

            foreach(int i in adj_tris)
            {
                flags[i * 3] &= (int)~Flag.AdjMask;
            }
            return error;
        }

        public void LockPostition(Vector3 pos)
        {
            uint p0 = MeshUtility.hash(pos);
            for(uint i = corner_ht.First(p0); corner_ht.IsValid(i); corner_ht.Next(i))
            {
                if(verts[indexes[i]] == pos)
                {
                    flags[i] |= (int)Flag.LockMask;
                }
            }
        }

        public void Simplify(int targetTriCount)
        {
            Array.Resize(ref tri_quadrics, num_tri);
            for(int i = 0; i < num_tri; i++)
            {
                FixupTriangle(i);
            }

            if(RemainingTriangleCount <= targetTriCount)
            {
                Console.WriteLine("Compacting Mesh");
                Compact();
                return;
            }
            heap.Resize(edges.Count);
            uint ii = 0;
            foreach((Vector3, Vector3) e in edges)
            {
                float error = Evaluate(e.Item1, e.Item2, false);
                heap.Add(error, ii);
                ii++;
            }

            MaxError = 0;
            while(!heap.Empty)
            {
                uint e_idx = heap.Top();
                if(heap.GetKey(e_idx) >= 1e6) break;

                heap.Pop();

                (Vector3, Vector3) e = edges[(int)e_idx];
                edge0_ht.Remove(MeshUtility.hash(e.Item1), e_idx);
                edge1_ht.Remove(MeshUtility.hash(e.Item2), e_idx);

                float error = Evaluate(e.Item1, e.Item2, true);
                if(error > MaxError) MaxError = error;

                if(RemainingTriangleCount <= targetTriCount) break;

                foreach(uint i in reevaluate_edge)
                {
                    (Vector3, Vector3) ee = edges[(int)i];
                    float error_b = Evaluate(ee.Item1, ee.Item2, false);
                    heap.Add(error_b, i);
                }
                Array.Clear(reevaluate_edge, 0, reevaluate_edge.Length);
            }
            Console.WriteLine("Compacting Mesh");
            Compact();
        }

        public void Compact()
        {
            int v_cnt = 0;
            for(int i = 0; i < num_vert; i++)
            {
                if(vert_refs[i] > 0)
                {
                    if(i != v_cnt) verts[v_cnt] = verts[i];
                    //Reuse subscript
                    vert_refs[i] = v_cnt++;
                }
            }
            //assert(v_cnt == remaining_num_vert);
            Console.WriteLine(v_cnt == RemainingVertexCount);

            int t_cnt = 0;
            for(int i = 0; i < num_tri; i++)
            {
                if(!tri_removed[i])
                {
                    for(int k = 0; k < 3; k++)
                    {
                        indexes[t_cnt * 3 + k] = vert_refs[indexes[i * 3 + k]];
                    }
                    t_cnt++;
                }
            }
            //assert(t_cnt == remaining_num_tri);
            Console.WriteLine(t_cnt == RemainingTriangleCount);
        }

        public void BeginMerge(Vector3 p)
        {
            uint h = MeshUtility.hash(p);
            for(uint i = vert_ht.First(h); vert_ht.IsValid(i); vert_ht.Next(i))
            {
                if(verts[i] == p)
                {
                    vert_ht.Remove(h, i);
                    move_vert.ToList().Add(i);
                }
            }

            for(uint i = corner_ht.First(h); corner_ht.IsValid(i); corner_ht.Next(i))
            {
                if(verts[indexes[i]] == p)
                {
                    corner_ht.Remove(h, i);
                    move_corner.ToList().Add(i);
                }
            }

            for(uint i = edge0_ht.First(h); edge0_ht.IsValid(i); edge0_ht.Next(i))
            {
                if(edges[(int)i].Item1 == p)
                {
                    edge0_ht.Remove(MeshUtility.hash(edges[(int)i].Item1), i);
                    edge1_ht.Remove(MeshUtility.hash(edges[(int)i].Item2), i);
                    move_edge.ToList().Add(i);
                }
            }

            for(uint i = edge1_ht.First(h); edge1_ht.IsValid(i); edge1_ht.Next(i))
            {
                if(edges[(int)i].Item2 == p)
                {
                    edge0_ht.Remove(MeshUtility.hash(edges[(int)i].Item1), i);
                    edge1_ht.Remove(MeshUtility.hash(edges[(int)i].Item2), i);
                    move_edge.ToList().Add(i);
                }
            }
        }

        public void EndMerge()
        {
            foreach(uint i in move_vert)
            {
                vert_ht.Add(MeshUtility.hash(verts[i]), i);
            }

            foreach(uint i in move_corner)
            {
                corner_ht.Add(MeshUtility.hash(verts[indexes[i]]), i);
            }

            foreach(uint i in move_edge)
            {
                (Vector3, Vector3) e = edges[(int)i];
                if(e.Item1 == e.Item2 || !AddEdgeht(e.Item1, e.Item2, i))
                {
                    heap.Remove(i);
                }
            }
            move_vert.ToList().Clear();
            move_corner.ToList().Clear();
            move_edge.ToList().Clear();
        }
    }
}
