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
        public struct Quadric
        {
            public double a2, b2, c2, d2;
            public double ab, ac, ad;
            public double bc, bd, cd;

            public double this[int index] { get { return 0.0; } }

            public Quadric(Vector3D p0, Vector3D p1, Vector3D p2)
            {
                Vector3D n = Vector3D.Cross(p1 - p0, p2 - p0).normalized;
                double a = n.x;
                double b = n.y;
                double c = n.z;
                double d = -Vector3D.Dot(n, p0);
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
                Matrix4x4D m = new Matrix4x4D(4), inv = new Matrix4x4D(4);
                m.SetColumn(0, new Vector4D(a2, ab, ac, 0.0));
                m.SetColumn(1, new Vector4D(ab, b2, bc, 0.0));
                m.SetColumn(2, new Vector4D(ac, bc, c2, 0.0));
                m.SetColumn(3, new Vector4D(ad, bd, cd, 1.0));
                if(!m.Invert(m, inv)) return false;
                Vector4D v = inv.column[3];
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

        private readonly Vector3[] Vertices;
        private readonly int[] Indexes;

        private readonly HashTable VertexHT;
        private readonly HashTable CornetHT;
        private readonly int[] VertexRefs;
        private readonly int[] Flags;
        private readonly BitArray TriangleRemoved;

        private enum Flag
        {
            AdjacencyMask = 1,
            LockMask = 2
        };

        private readonly List<Pair<Vector3, Vector3>> Edges;
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
            Vertices = vertices;
            Indexes = triangles;
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
                VertexHT.Add(TriangleUtils.Hash(Vertices[i]), i);
            }
            MoveVertex = new uint[0];
            MoveCorner = new uint[0];
            MoveEdge = new uint[0];
            ReevaluateEdge = new uint[0];

            // Guess number of edges based on Euler's formula.
            uint NumEdges = MathUtils.Min3((uint)NumIndicies, (uint)(3 * NumVerticies - 6), (uint)(NumTriangles + NumVerticies));
            Console.WriteLine("[MeshSimplifier] Num Edges: " + NumEdges);
            Edges = new List<Pair<Vector3, Vector3>>();
            ListUtils.Reserve(Edges, NumEdges);
            Edge0HT = new HashTable(NumEdges);
            Edge0HT.Clear((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(NumEdges, 2)))), NumEdges);
            Edge1HT = new HashTable(NumEdges);
            Edge1HT.Clear((uint)(1 << System.Math.Min(16, (int)System.Math.Floor(System.Math.Log(NumEdges, 2)))), NumEdges);

            for(uint corner = 0u; corner < NumIndicies; corner++)
            {
                int vertIndex = Indexes[corner];
                VertexRefs[vertIndex]++;
                Vector3 p = Vertices[vertIndex];
                CornetHT.Add(TriangleUtils.Hash(p), corner);
                Vector3 p1 = Vertices[Indexes[TriangleUtils.Cycle3(corner)]];
                if(AddEdgeht(p, p1, (uint)Edges.Count))
                {
                    Edges.Add(new Pair<Vector3, Vector3>(p, p1));
                }
            }
            Console.WriteLine("[MeshSimplifier] Finished Initialization");
        }

        public bool AddEdgeht(Vector3 p0, Vector3 p1, uint index)
        {
            uint h0 = TriangleUtils.Hash(p0), h1 = TriangleUtils.Hash(p1);
            if(h0 > h1)
            {
                (h0, h1) = (h1, h0);
                (p0, p1) = (p1, p0);
            }

            foreach(uint i in Edge0HT[h0])
            {
                Pair<Vector3, Vector3> kv = Edges[(int)i];
                if(kv.Key == p0 && kv.Value == p1)
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
            uint v_idx = (uint)Indexes[corner];
            Debug.Assert(v_idx != ~0u);
            Debug.Assert(VertexRefs[v_idx] > 0);

            if(v_idx == index)
                return;
            if(--VertexRefs[v_idx] == 0)
            {
                VertexHT.Remove(TriangleUtils.Hash(Vertices[v_idx]), v_idx);
                RemainingVertexCount--;
            }
            v_idx = index;
            if(v_idx != ~0u)
                VertexRefs[v_idx]++;
        }

        // Assign corner to the Keys.ToList()[0] encourntered identical point
        public void RemoveIfVertexDuplicate(uint corner)
        {
            int v_idx = Indexes[corner];
            Vector3 v = Vertices[v_idx];
            uint v0 = TriangleUtils.Hash(v);
            foreach(uint i in VertexHT[v0])
            {
                if(i == v_idx)
                    break;
                if(v == Vertices[i])
                {
                    SetVertexIndex(corner, i);
                    break;
                }
            }
        }

        public bool IsTriangleDuplicate(int triangleIndex)
        {
            int i0 = Indexes[triangleIndex * 3 + 0], i1 = Indexes[triangleIndex * 3 + 1], i2 = Indexes[triangleIndex * 3 + 2];
            uint v0 = TriangleUtils.Hash(Vertices[0]);
            foreach(uint i in CornetHT[v0])
            {
                if(i != triangleIndex * 3)
                {
                    if(i0 == Indexes[i] && i1 == Indexes[TriangleUtils.Cycle3(i)] && i2 == Indexes[TriangleUtils.Cycle3(i, 2)])
                        return true;
                }
            }
            return false;
        }

        public void FixupTriangle(int tri_idx)
        {
            Debug.Assert(!TriangleRemoved[tri_idx]);

            Vector3 p0 = Vertices[Indexes[tri_idx * 3 + 0]];
            Vector3 p1 = Vertices[Indexes[tri_idx * 3 + 1]];
            Vector3 p2 = Vertices[Indexes[tri_idx * 3 + 2]];

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
                    int v_idx = Indexes[corner];
                    CornetHT.Remove(TriangleUtils.Hash(Vertices[v_idx]), corner);
                    SetVertexIndex(corner, ~0u);
                }
            }

            else
                TriangleQuadrics[tri_idx] = new Quadric(new Vector3D(p0.x, p0.y, p0.z), new Vector3D(p1.x, p1.y, p1.z), new Vector3D(p2.x, p2.y, p2.z));
        }

        public void GatherAdjacentTriangles(Vector3 p, int[] triangles, out bool Lock)
        {
            Lock = false;
            uint p0 = TriangleUtils.Hash(p);
            foreach(uint i in CornetHT[p0])
            {
                if(Vertices[Indexes[i]] == p)
                {
                    int tri_idx = (int)i / 3;
                    if((Flags[tri_idx * 3] & (int)Flag.AdjacencyMask) == 0)
                    {
                        Flags[tri_idx * 3] |= (int)Flag.AdjacencyMask;
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
                        Vector3 pos = Vertices[Indexes[corner]];
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
                    Pair<Vector3, Vector3> kv = Edges[(int)i];
                    if(kv.Key == p0 || kv.Key == p1)
                        kv.Key = p;
                    if(kv.Value == p0 || kv.Value == p1)
                        kv.Value = p;
                }
                EndMerge();

                int[] adj_verts = new int[0];
                foreach(int i in adj_tris)
                {
                    for(int k = 0; k < 3; k++)
                    {
                        adj_verts.Append(Indexes[i * 3 + k]);
                    }
                }
                Array.Sort(adj_verts, 0, adj_verts.Length);
                adj_verts.ToList().RemoveRange(0, adj_verts.Length);

                foreach(int v_idx in adj_verts)
                {
                    uint h = TriangleUtils.Hash(Vertices[v_idx]);
                    foreach(uint i in Edge0HT[h])
                    {
                        if(Edges[(int)i].Key == Vertices[v_idx])
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
                        if(Edges[(int)i].Value == Vertices[v_idx])
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
                    FixupTriangle(i);

                /*Parallel.ForEach(adj_tris, i =>
                {
                    FixupTriangle(i);
                });*/
            }

            foreach(int i in adj_tris)
            {
                Flags[i * 3] &= (int)~Flag.AdjacencyMask;
            }
            return error;
        }

        public void LockPosition(Vector3 pos)
        {
            uint p0 = TriangleUtils.Hash(pos);
            foreach(uint i in CornetHT[p0])
            {
                if(Vertices[Indexes[i]] == pos)
                {
                    Flags[i] |= (int)Flag.LockMask;
                }
            }
        }

        public void Simplify(int targetTriCount)
        {
            Console.WriteLine("[MeshSimplifier] Begin Triangle Fixup");
            Array.Resize(ref TriangleQuadrics, NumTriangles);

            Parallel.For(0, NumTriangles, i =>
            {
                FixupTriangle(i);
            });
            Console.WriteLine("[MeshSimplifier] End Triangle Fixup");

            if(RemainingTriangleCount <= targetTriCount)
            {
                Compact();
                return;
            }
            Heap.Resize(Edges.Count);
            uint ii = 0;
            foreach(Pair<Vector3, Vector3> kv in Edges)
            {
                float error = Evaluate(kv.Key, kv.Value, false);
                Heap.Add(error, ii);
                ii++;
            }

            MaxError = 0;
            while(!Heap.Empty)
            {
                uint e_idx = Heap.Top();
                if(Heap.GetKey(e_idx) >= 1e6f) break;

                Heap.Pop();

                Pair<Vector3, Vector3> kv = Edges[(int)e_idx];
                Edge0HT.Remove(TriangleUtils.Hash(kv.Key), e_idx);
                Edge1HT.Remove(TriangleUtils.Hash(kv.Value), e_idx);

                float error = Evaluate(kv.Key, kv.Value, true);
                if(error > MaxError)
                    MaxError = error;

                if(RemainingTriangleCount <= targetTriCount)
                    break;

                foreach(uint i in ReevaluateEdge)
                {
                    Pair<Vector3, Vector3> kv2 = Edges[(int)i];
                    float error_b = Evaluate(kv2.Key, kv2.Value, false);
                    Heap.Add(error_b, i);
                }
                Array.Clear(ReevaluateEdge, 0, ReevaluateEdge.Length);
            }
            Compact();
        }

        public void Compact()
        {
            Console.WriteLine("[MeshSimplifier] Begin Compacting");
            int v_cnt = 0;
            for(int i = 0; i < NumVerticies; i++)
            {
                if(VertexRefs[i] > 0)
                {
                    if(i != v_cnt)
                        Vertices[v_cnt] = Vertices[i];
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
                        Indexes[t_cnt * 3 + k] = VertexRefs[Indexes[i * 3 + k]];
                    }
                    t_cnt++;
                }
            }
            Debug.Assert(t_cnt == RemainingTriangleCount);
            Console.WriteLine("[MeshSimplifier] End Compacting");
        }

        public void BeginMerge(Vector3 p)
        {
            uint h = TriangleUtils.Hash(p);
            foreach(uint i in VertexHT[h])
            {
                if(Vertices[i] == p)
                {
                    VertexHT.Remove(h, i);
                    MoveVertex.Append(i);
                }
            }

            foreach(uint i in CornetHT[h])
            {
                if(Vertices[Indexes[i]] == p)
                {
                    CornetHT.Remove(h, i);
                    MoveCorner.Append(i);
                }
            }

            foreach(uint i in Edge0HT[h])
            {
                if(Edges[(int)i].Key == p)
                {
                    Edge0HT.Remove(TriangleUtils.Hash(Edges[(int)i].Key), i);
                    Edge1HT.Remove(TriangleUtils.Hash(Edges[(int)i].Value), i);
                    MoveEdge.Append(i);
                }
            }

            foreach(uint i in Edge1HT[h])
            {
                if(Edges[(int)i].Value == p)
                {
                    Edge0HT.Remove(TriangleUtils.Hash(Edges[(int)i].Key), i);
                    Edge1HT.Remove(TriangleUtils.Hash(Edges[(int)i].Value), i);
                    MoveEdge.Append(i);
                }
            }
        }

        public void EndMerge()
        {
            foreach(uint i in MoveVertex)
            {
                VertexHT.Add(TriangleUtils.Hash(Vertices[i]), i);
            }

            foreach(uint i in MoveCorner)
            {
                CornetHT.Add(TriangleUtils.Hash(Vertices[Indexes[i]]), i);
            }

            foreach(uint i in MoveEdge)
            {
                Pair<Vector3, Vector3> kv = Edges[(int)i];
                if(kv.Key == kv.Value || !AddEdgeht(kv.Value, kv.Value, i))
                {
                    Heap.Remove(i);
                }
            }
            MoveVertex.ToList().Clear();
            MoveCorner.ToList().Clear();
            MoveEdge.ToList().Clear();
        }

        #region Max Area Triangulation
        /// <summary>
        /// Max Area Triangulation method to greedly triangulate the surface to maximize surface area
        /// </summary>
        public void MaxAreaTriangulate()
        {
            HashSet<int> remainingVertices = new HashSet<int>(Enumerable.Range(0, Vertices.Length));

            while(remainingVertices.Count >= 3 && RemainingTriangleCount > 0)
            {
                double maxArea = double.NegativeInfinity;
                int bestA = -1, bestB = -1, bestC = -1;

                foreach(int i in remainingVertices)
                {
                    foreach(int j in remainingVertices)
                    {
                        if(j == i) continue;

                        foreach(int k in remainingVertices)
                        {
                            if(k == i || k == j) continue;

                            Vector3 a = Vertices[i];
                            Vector3 b = Vertices[j];
                            Vector3 c = Vertices[k];
                            double area = CalculateTriangleArea(a, b, c);

                            if(area > maxArea && area > 1e-6) // Avoid degenerate triangles
                            {
                                if(!IsTriangleDuplicate(i / 3))
                                {
                                    maxArea = area;
                                    bestA = i;
                                    bestB = j;
                                    bestC = k;
                                }
                            }
                        }
                    }
                }

                if(bestA != -1 && bestB != -1 && bestC != -1)
                {
                    // Add the triangle with the largest area
                    AddTriangle(bestA, bestB, bestC);

                    // Remove these vertices from the remaining vertices set
                    remainingVertices.Remove(bestA);
                    remainingVertices.Remove(bestB);
                    remainingVertices.Remove(bestC);
                }

                else
                {
                    break;
                }
            }
        }

        // Helper method to calculate the area of a triangle in 3D
        double CalculateTriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 crossProduct = Vector3.Cross(ab, ac);
            return 0.5 * crossProduct.magnitude;
        }

        // Helper method to add a triangle to the mesh and HashTables
        private void AddTriangle(int indexA, int indexB, int indexC)
        {
            // Add the triangle indices to Indexes array
            int triIndex = RemainingTriangleCount * 3;
            Indexes[triIndex + 0] = indexA;
            Indexes[triIndex + 1] = indexB;
            Indexes[triIndex + 2] = indexC;

            // Add to CornetHT to prevent duplicates
            CornetHT.Add(TriangleUtils.Hash(Vertices[indexA]), (uint)(triIndex + 0));
            CornetHT.Add(TriangleUtils.Hash(Vertices[indexB]), (uint)(triIndex + 1));
            CornetHT.Add(TriangleUtils.Hash(Vertices[indexC]), (uint)(triIndex + 2));

            RemainingTriangleCount++;
        }
        #endregion
    }
}
