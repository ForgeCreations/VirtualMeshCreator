using ShellProgressBar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
                Array.Clear(new double[10]
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

            public bool Get(ref Vector3 p)
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

        private readonly int NumVerticies;
        private readonly int NumIndicies;
        private readonly int NumTriangles;

        private readonly Vector3[] verticies;
        private readonly int[] indexes;

        private readonly HashTable VertexHT;
        private readonly HashTable CornetHT;
        private readonly int[] VertexRefs;
        private readonly int[] Flags;
        private readonly BitArray TriangleRemoved;

        private enum Flag
        {
            AdjMask = 1,
            LockMask = 2
        };

        private readonly List<(Vector3 v0, Vector3 v1)> Edges;
        private readonly HashTable Edge0HT;
        private readonly HashTable Edge1HT;
        private readonly Heap Heap;

        private readonly uint[] MoveVertex;
        private readonly uint[] MoveCorner;
        private readonly uint[] MoveEdge;
        private readonly uint[] ReevaluateEdge;

        private Quadric[] TriangleQuadrics;

        public int RemainingVertexCount { get; private set; } = 0;
        public int RemainingTriangleCount { get; private set; } = 0;
        public float MaxError { get; private set; } = 0;

        public MeshSimplifier(Vector3[] vertices, int vertexCount, int[] triangles, int triangleCount)
        {
            NumVerticies = vertexCount;
            NumIndicies = triangleCount;
            NumTriangles = NumIndicies / 3;
            verticies = vertices;
            indexes = triangles;
            VertexHT = new HashTable((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(NumVerticies, 2.0)))));
            VertexRefs = new int[NumVerticies];
            Array.Clear(VertexRefs, 0, NumVerticies);
            CornetHT = new HashTable((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(NumIndicies, 2.0)))));
            TriangleRemoved = new BitArray(NumTriangles);
            Flags = new int[NumIndicies];
            Array.Clear(Flags, 0, NumIndicies);
            RemainingVertexCount = NumVerticies;
            RemainingTriangleCount = NumTriangles;
            for(uint i = 0; i < NumVerticies; i++)
            {
                VertexHT.Add(MeshUtility.Hash(verticies[i]), i);
            }

            // Guess number of edges based on Euler's formula.
            uint NumEdges = MathUtils.Min3((uint)NumIndicies, (uint)(3 * NumVerticies - 6), (uint)(NumTriangles + NumVerticies));
            Console.WriteLine("[MeshSimplifier] Num Edges: " + NumEdges);
            Edges = new List<(Vector3, Vector3)>();
            ListUtils.Reserve(Edges, NumEdges);
            Edge0HT = new HashTable(NumEdges);
            Edge0HT.Clear((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(NumEdges, 2.0)))), NumEdges);
            Edge1HT = new HashTable(NumEdges);
            Edge1HT.Clear((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(NumEdges, 2.0)))), NumEdges);

            for(uint corner = 0u; corner < NumIndicies; corner++)
            {
                int vertIndex = indexes[corner];
                VertexRefs[vertIndex]++;
                Vector3 p = verticies[vertIndex];
                CornetHT.Add(MeshUtility.Hash(p), corner);
                (Vector3 v0, Vector3 v1) vPair = (p, verticies[indexes[MeshUtility.Cycle3(corner)]]);
                if(AddEdgeht(vPair.v0, vPair.v1, (uint)Edges.Count))
                {
                    Edges.Add(vPair);
                }
            }

            /*Parallel.For(0u, NumIndicies, corner =>
            {
                int vertIndex = indexes[corner];
                VertexRefs[vertIndex]++;
                Vector3 p = verticies[vertIndex];
                CornetHT.Add(MeshUtility.Hash(p), (uint)corner);
                (Vector3 v0, Vector3 v1) vPair = (p, verticies[indexes[MeshUtility.Cycle3((uint)corner)]]);
                if(AddEdgeht(vPair.v0, vPair.v1, (uint)Edges.Count))
                {
                    Edges.Add(vPair);
                }
            });*/
            Console.WriteLine("[MeshSimplifier] Finished Initialization");
        }

        public bool AddEdgeht(Vector3 p0, Vector3 p1, uint index)
        {
            uint h0 = MeshUtility.Hash(p0), h1 = MeshUtility.Hash(p1);
            if(h0 > h1)
            {
                ArrayUtils.Swap(ref h0, ref h1);
                ArrayUtils.Swap(ref p0, ref p1);
            }

            foreach(uint i in Edge0HT[h0])
            {
                (Vector3 v0, Vector3 v1) = Edges[(int)i];
                if(v0 == p0 && v1 == p1)
                {
                    return false; // Found a duplicate
                }
            }

            Edge0HT.Add(h0, index);
            Edge1HT.Add(h1, index);
            return true;
        }

        public void SetVertexIndex(uint corner, uint index)
        {
            uint v_idx = (uint)indexes[corner];
            Debug.Assert(v_idx != ~0u);
            Debug.Assert(VertexRefs[v_idx] > 0);

            if(v_idx == index)
                return;
            if(--VertexRefs[v_idx] == 0)
            {
                VertexHT.Remove(MeshUtility.Hash(verticies[v_idx]), v_idx);
                RemainingVertexCount--;
            }
            v_idx = index;
            if(v_idx != ~0u)
                VertexRefs[v_idx]++;
        }

        // Assign corner to the Keys.ToList()[0] encourntered identical point
        public void RemoveIfVertexDuplicate(uint corner)
        {
            int v_idx = indexes[corner];
            Vector3 v = verticies[v_idx];
            uint v0 = MeshUtility.Hash(v);
            foreach(uint i in VertexHT[v0])
            {
                if(i == v_idx)
                    break;
                if(v == verticies[i])
                {
                    SetVertexIndex(corner, i);
                    break;
                }
            }
        }

        public bool IsTriangleDuplicate(int triangleIndex)
        {
            int i0 = indexes[triangleIndex * 3 + 0], i1 = indexes[triangleIndex * 3 + 1], i2 = indexes[triangleIndex * 3 + 2];
            uint v0 = MeshUtility.Hash(verticies[0]);
            foreach(uint i in CornetHT[v0])
            {
                if(i != triangleIndex * 3)
                {
                    if(i0 == indexes[i] && i1 == indexes[MeshUtility.Cycle3(i)] && i2 == indexes[MeshUtility.Cycle3(i, 2)])
                        return true;
                }
            }
            return false;
        }

        public void FixupTriangle(int tri_idx)
        {
            Debug.Assert(!TriangleRemoved[tri_idx]);

            Vector3 p0 = verticies[indexes[tri_idx * 3 + 0]];
            Vector3 p1 = verticies[indexes[tri_idx * 3 + 1]];
            Vector3 p2 = verticies[indexes[tri_idx * 3 + 2]];

            bool is_removed = false;
            if(!is_removed)
            {
                is_removed = (p0 == p1) || (p1 == p2) || (p2 == p0);
            }

            if(!is_removed)
            {
                for(int k = 0; k < 3; k++)
                    RemoveIfVertexDuplicate((uint)(tri_idx * 3 + k));
                is_removed = IsTriangleDuplicate(tri_idx);
            }

            if(is_removed)
            {
                TriangleRemoved.Set(tri_idx, true);
                RemainingTriangleCount--;
                for(int k = 0; k < 3; k++)
                {
                    uint corner = (uint)(tri_idx * 3 + k);
                    int v_idx = indexes[corner];
                    CornetHT.Remove(MeshUtility.Hash(verticies[v_idx]), corner);
                    SetVertexIndex(corner, ~0u);
                }
            }

            else
                TriangleQuadrics[tri_idx] = new Quadric(new DVector3(p0.x, p0.y, p0.z), new DVector3(p1.x, p1.y, p1.z), new DVector3(p2.x, p2.y, p2.z));
        }

        public void GatherAdjacentTriangles(Vector3 p, int[] triangles, out bool Lock)
        {
            Lock = false;
            uint p0 = MeshUtility.Hash(p);
            foreach(uint i in CornetHT[p0])
            {
                if(verticies[indexes[i]] == p)
                {
                    int tri_idx = (int)i / 3;
                    if((Flags[tri_idx * 3] & (int)Flag.AdjMask) == 0)
                    {
                        Flags[tri_idx * 3] |= (int)Flag.AdjMask;
                        triangles.Append(tri_idx);
                    }

                    if((Flags[i] & (int)Flag.LockMask) == 1)
                    {
                        Lock = true;
                    }
                }
            }
        }

        public float Evaluate(Vector3 p0, Vector3 p1, bool merge)
        {
            if(p0 == p1)
                return 0.0f;

            float error = 0.0f;

            int[] adj_tris = new int[0];
            GatherAdjacentTriangles(p0, adj_tris, out bool lock0);
            GatherAdjacentTriangles(p1, adj_tris, out bool lock1);
            if(adj_tris.Length == 0)
                return 0.0f;
            if(adj_tris.Length > 24)
            {
                error += 0.5f * (adj_tris.Length - 24);
            }

            Quadric q = new Quadric();
            foreach(int i in adj_tris)
            {
                q.Add(TriangleQuadrics[i]);
            }
            Vector3 p = (p0 + p1) * 0.5f;

            bool is_valid_pos = (p - p0).magnitude + (p - p1).magnitude > 2 * (p0 - p1).magnitude;

            if(lock0 && lock1)
                error += 1e8f;
            if(lock0 && !lock1)
                p = p0;
            else if(!lock0 && lock1)
                p = p1;
            else if(!q.Get(ref p))
                p = (p0 + p1) * 0.5f;
            if(!is_valid_pos)
            {
                p = (p0 + p1) * 0.5f;
            }
            error += q.Evaluate(p);

            if(merge)
            {
                BeginMerge(p0); BeginMerge(p1);
                foreach(int i in adj_tris)
                {
                    for(int k = 0; k < 3; k++)
                    {
                        int corner = i * 3 + k;
                        Vector3 pos = verticies[indexes[corner]];
                        if(pos == p0 || pos == p1)
                        {
                            pos = p;
                            if(lock0 || lock1)
                                Flags[corner] |= (int)Flag.LockMask;
                        }
                    }
                }

                foreach(uint i in MoveEdge)
                {
                    (Vector3 v0, Vector3 v1) e = Edges[(int)i];
                    if(e.v0 == p0 || e.v0 == p1)
                        e.v0 = p;
                    if(e.v1 == p0 || e.v1 == p1)
                        e.v1 = p;
                }
                EndMerge();

                int[] adj_verts = new int[0];
                foreach(int i in adj_tris)
                {
                    for(int k = 0; k < 3; k++)
                    {
                        adj_verts.Append(indexes[i * 3 + k]);
                    }
                }
                Array.Sort(adj_verts, 0, adj_verts.Length);
                adj_verts.ToList().RemoveRange(0, adj_verts.Length);

                foreach(int v_idx in adj_verts)
                {
                    uint h = MeshUtility.Hash(verticies[v_idx]);
                    foreach(uint i in Edge0HT[h])
                    {
                        if(Edges[(int)i].v0 == verticies[v_idx])
                        {
                            if(Heap.IsPresent(i))
                            {
                                Heap.Remove(i);
                                ReevaluateEdge.Append(i);
                            }
                        }
                    }

                    foreach(uint i in Edge1HT[h])
                    {
                        if(Edges[(int)i].v1 == verticies[v_idx])
                        {
                            if(Heap.IsPresent(i))
                            {
                                Heap.Remove(i);
                                ReevaluateEdge.Append(i);
                            }
                        }
                    }
                }

                foreach(int i in adj_tris)
                {
                    FixupTriangle(i);
                }

                /*Parallel.ForEach(adj_tris, i =>
                {
                    FixupTriangle(i);
                });*/
            }

            foreach(int i in adj_tris)
            {
                Flags[i * 3] &= (int)~Flag.AdjMask;
            }
            return error;
        }

        public void LockPosition(Vector3 pos)
        {
            uint p0 = MeshUtility.Hash(pos);
            foreach(uint i in CornetHT[p0])
            {
                if(verticies[indexes[i]] == pos)
                {
                    Flags[i] |= (int)Flag.LockMask;
                }
            }
        }

        public void Simplify(int targetTriCount)
        {
            Console.WriteLine("[MeshSimplifier] Begin Triangle Fixup");
            Array.Resize(ref TriangleQuadrics, NumTriangles);
            /*for(int i = 0; i < NumTriangles; i++)
            {
                FixupTriangle(i);
                Console.Write($"[MeshSimplifier] Fixup Triangle Index: {i}\n");
            }*/

            Parallel.For(0, NumTriangles, i =>
            {
                FixupTriangle(i);
                //Console.WriteLine($"[MeshSimplifier] Fixup Triangle Index: {i}");
            });
            Console.WriteLine("[MeshSimplifier] End Triangle Fixup");

            if(RemainingTriangleCount <= targetTriCount)
            {
                Compact();
                return;
            }
            Heap.Resize(Edges.Count);
            uint ii = 0;
            foreach((Vector3 v0, Vector3 v1) e in Edges)
            {
                float error = Evaluate(e.v0, e.v1, false);
                Heap.Add(error, ii);
                ii++;
            }

            MaxError = 0;
            while(!Heap.Empty)
            {
                uint e_idx = Heap.Top();
                if(Heap.GetKey(e_idx) >= 1e6f) break;

                Heap.Pop();

                (Vector3 v0, Vector3 v1) e = Edges[(int)e_idx];
                Edge0HT.Remove(MeshUtility.Hash(e.v0), e_idx);
                Edge1HT.Remove(MeshUtility.Hash(e.v1), e_idx);

                float error = Evaluate(e.v0, e.v1, true);
                if(error > MaxError)
                    MaxError = error;

                if(RemainingTriangleCount <= targetTriCount)
                    break;

                foreach(uint i in ReevaluateEdge)
                {
                    (Vector3 v0, Vector3 v1) ee = Edges[(int)i];
                    float error_b = Evaluate(ee.v0, ee.v1, false);
                    Heap.Add(error_b, i);
                }
                Array.Clear(ReevaluateEdge, 0, ReevaluateEdge.Length);
            }
            Compact();
        }

        public void Compact()
        {
            Console.WriteLine("[MeshSimplifier] Compacting Mesh");
            int v_cnt = 0;
            for(int i = 0; i < NumVerticies; i++)
            {
                if(VertexRefs[i] > 0)
                {
                    if(i != v_cnt)
                        verticies[v_cnt] = verticies[i];
                    // Reuse subscript
                    VertexRefs[i] = v_cnt++;
                }
            }
            Debug.Assert(v_cnt == RemainingVertexCount);

            int t_cnt = 0;
            for(int i = 0; i < NumTriangles; i++)
            {
                if(!TriangleRemoved[i])
                {
                    for(int k = 0; k < 3; k++)
                    {
                        indexes[t_cnt * 3 + k] = VertexRefs[indexes[i * 3 + k]];
                    }
                    t_cnt++;
                }
            }
            Debug.Assert(t_cnt == RemainingTriangleCount);
            Console.WriteLine("[MeshSimplifier] End Mesh Compacting");
        }

        public void BeginMerge(Vector3 p)
        {
            uint h = MeshUtility.Hash(p);
            foreach(uint i in VertexHT[h])
            {
                if(verticies[i] == p)
                {
                    VertexHT.Remove(h, i);
                    MoveVertex.Append(i);
                }
            }

            foreach(uint i in CornetHT[h])
            {
                if(verticies[indexes[i]] == p)
                {
                    CornetHT.Remove(h, i);
                    MoveCorner.Append(i);
                }
            }

            foreach(uint i in Edge0HT[h])
            {
                if(Edges[(int)i].v0 == p)
                {
                    Edge0HT.Remove(MeshUtility.Hash(Edges[(int)i].v0), i);
                    Edge1HT.Remove(MeshUtility.Hash(Edges[(int)i].v1), i);
                    MoveEdge.Append(i);
                }
            }

            foreach(uint i in Edge1HT[h])
            {
                if(Edges[(int)i].v1 == p)
                {
                    Edge0HT.Remove(MeshUtility.Hash(Edges[(int)i].v0), i);
                    Edge1HT.Remove(MeshUtility.Hash(Edges[(int)i].v1), i);
                    MoveEdge.Append(i);
                }
            }
        }

        public void EndMerge()
        {
            foreach(uint i in MoveVertex)
            {
                VertexHT.Add(MeshUtility.Hash(verticies[i]), i);
            }

            foreach(uint i in MoveCorner)
            {
                CornetHT.Add(MeshUtility.Hash(verticies[indexes[i]]), i);
            }

            foreach(uint i in MoveEdge)
            {
                (Vector3 v0, Vector3 v1) e = Edges[(int)i];
                if(e.v0 == e.v1 || !AddEdgeht(e.v1, e.v1, i))
                {
                    Heap.Remove(i);
                }
            }
            MoveVertex.ToList().Clear();
            MoveCorner.ToList().Clear();
            MoveEdge.ToList().Clear();
        }

        #region Max Area Triangulation
        private void TriangulateSurfaceArea(Vector3[] vertices, ref int[] triangles)
        {
            List<int> remainingVertices = Enumerable.Range(0, vertices.ToList().Count).ToList();

            while(remainingVertices.Count >= 3)
            {
                double maxArea = double.NegativeInfinity;
                List<int> bestTriangle = null;

                // Try every combination of 3 vertices from the remaining vertices
                for(int i = 0; i < remainingVertices.Count - 2; i++)
                {
                    for(int j = i + 1; j < remainingVertices.Count - 1; j++)
                    {
                        for(int k = j + 1; k < remainingVertices.Count; k++)
                        {
                            int indexA = remainingVertices[i];
                            int indexB = remainingVertices[j];
                            int indexC = remainingVertices[k];

                            double area = CalculateTriangleArea(vertices[indexA], vertices[indexB], vertices[indexC]);

                            if(area > maxArea & (area != 0))
                            {
                                maxArea = area;
                                bestTriangle = new List<int> { indexA, indexB, indexC };
                            }
                        }
                    }
                }

                if(bestTriangle != null)
                {
                    triangles.Append(bestTriangle[0]);
                    triangles.Append(bestTriangle[1]);
                    triangles.Append(bestTriangle[2]);

                    // Remove the vertices in reverse order to avoid index shifting issues
                    remainingVertices.Remove(bestTriangle[2]);
                    remainingVertices.Remove(bestTriangle[1]);
                    remainingVertices.Remove(bestTriangle[0]);
                }

                else
                {
                    break;
                }
            }
        }

        double CalculateTriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 crossProduct = Vector3.Cross(ab, ac);
            return 0.5 * crossProduct.magnitude;
        }
        #endregion
    }
}
