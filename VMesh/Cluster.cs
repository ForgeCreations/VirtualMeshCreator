using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualMeshCreator.Math;
using VirtualMeshCreator.Utility;

namespace VirtualMeshCreator.VMesh
{
    public class Cluster
    {
        public static readonly int CLUSTER_SIZE = 128;

        public int groupID = int.MaxValue;
        public int groupPartID = int.MaxValue;
        public int generatingGroupID = int.MaxValue;
        public int mipLevel;
        public float edgeLength = 0.0f;
        public float lodError = 0.0f;
        public float surfaceArea = 0.0f;
        public Bounds boxBounds; // Depricated
        public Sphere sphereBounds;
        public Sphere lodBounds;

        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
        public Vector3[] normals;
        public int[] externalEdges;

        public Vector3 QuantizedPosStart = Vector3.zero;
        public uint QuantizedPosPrecision = 0u;
        public Vector3 QuantizedPosBits = Vector3.zero;

        // Edge Hashing, finds the opposite edges that share a vertex, indicating that two triangles are adjacent
        public static void BuildAdjacencyEdgeLink(ref Vector3[] vertices, ref int[] triangles, out Graph edgeLink)
        {
            HashTable edge_ht = new HashTable((uint)triangles.Length);
            edgeLink = new Graph();
            edgeLink.Init(triangles.Length);

            for(uint i = 0; i < triangles.Length; i++)
            {
                Vector3 p0 = vertices[triangles[i]];
                Vector3 p1 = vertices[triangles[MeshUtility.Cycle3(i)]];
                uint p01 = MeshUtility.Hash((p0, p1));
                edge_ht.Add(p01, i);

                foreach(uint j in edge_ht[p01])
                {
                    if(p1 == vertices[triangles[j]] && p0 == vertices[triangles[MeshUtility.Cycle3(j)]])
                    {
                        edgeLink.IncreaseEdgeCost(i, j, 1);
                        edgeLink.IncreaseEdgeCost(j, i, 1);
                    }
                }
            }
        }

        // TODO: After Morton codes are sorted, edges are connected between different connected blocks to ensure grid connectivity.
        public Graph BuildLocalityLinksTriangles(Vector3[] verts, int[] indexes)
        {
            Graph graph = new Graph();
            Bounds bounds = new Bounds(verts[0], verts[0]);
            foreach (Vector3 p in verts)
                bounds += p;
            Vector3 extent = bounds.Max - bounds.Min;
            float maxLength = System.Math.Max(System.Math.Max(extent.x, extent.y), extent.z);

            for (int i = 0; i < indexes.Length / 3; i++)
            {
                Vector3 p0 = verts[indexes[i * 3]];
                Vector3 p1 = verts[indexes[i * 3] + 1];
                Vector3 p2 = verts[indexes[i * 3] + 2];
                Vector3 center = (p0 + p1 + p2) * (1.0f / 3.0f);
                center = (center - bounds.Min) * (1.0f / maxLength);

                uint morton = MeshUtility.Morton3D(center);
            }
            return graph;
        }

        // Contruct a triangle adjacency graph based on the edge adjacency. The edge weight is 1. When local needs to be added, the adjacency edge needs to be large enough.
        public static void BuildAdjacencyGraph(Graph edgeLink, out Graph graph)
        {
            graph = new Graph();
            graph.Init(edgeLink.g.Count / 3);
            uint u = 0;
            foreach(Dictionary<uint, int> emp in edgeLink.g)
            {
                foreach(KeyValuePair<uint, int> kv in emp)
                {
                    graph.IncreaseEdgeCost(u / 3, kv.Key / 3, 1);
                }
                u++;
            }
        }

        public static void ClusterTriangles(ref Vector3[] vertices, ref int[] triangles, ref Cluster[] clusters)
        {
            BuildAdjacencyEdgeLink(ref vertices, ref triangles, out Graph edgeLink);
            BuildAdjacencyGraph(edgeLink, out Graph graph);

            Partitioner partitioner = new Partitioner();
            partitioner.Partition(graph, CLUSTER_SIZE - 4, CLUSTER_SIZE);

            foreach(Pair<int, int> lr in partitioner.ranges)
            {
                clusters.Append(new Cluster());
                Cluster cluster = clusters.Last();

                Dictionary<uint, uint> mp = new Dictionary<uint, uint>();
                for(uint i = (uint)lr.Key; i < lr.Value; i++)
                {
                    uint t_idx = partitioner.nodeIDs[i];
                    for(uint k = 0; k < 3; k++)
                    {
                        uint e_idx = t_idx * 3 + k;
                        int v_idx = triangles[e_idx];
                        if(Utils.KVEquals(Utils.Find(mp, (uint)v_idx), mp.Last()))
                        {
                            // Remap vertex subscripts
                            mp[(uint)v_idx] = (uint)cluster.vertices.Length;
                            cluster.vertices.Append(vertices[v_idx]);
                        }
                        bool is_external = false;
                        foreach(KeyValuePair<uint, int> gg in edgeLink.g[(int)e_idx])
                        {
                            uint adj_tri = partitioner.sortTo[gg.Key / 3];
                            if(adj_tri < lr.Key || adj_tri >= lr.Value)
                            {
                                // The output points are defined as boundaries in different divisions.
                                is_external = true;
                                break;
                            }
                        }

                        if(is_external)
                        {
                            cluster.externalEdges.Append(cluster.triangles.Length);
                        }
                        cluster.triangles.Append((int)mp[(uint)v_idx]);
                    }
                }

                cluster.mipLevel = 0;
                cluster.lodError = 0;
                cluster.sphereBounds = Sphere.FromPoints(cluster.vertices, cluster.vertices.Length);
                cluster.lodBounds = cluster.sphereBounds;
                cluster.boxBounds = new Bounds(cluster.vertices[0], cluster.vertices[0]);
                foreach(Vector3 p in cluster.vertices)
                    cluster.boxBounds += p;
            }
        }

        public static void BuildClustersEdgeLink(Cluster[] clusters, Pair<uint, uint>[] ext_edges, Graph edgeLink)
        {
            HashTable edge_ht = new HashTable((uint)ext_edges.Length);
            edgeLink.Init(ext_edges.Length);

            uint i = 0;
            foreach(Pair<uint, uint> ce in ext_edges)
            {
                Vector3[] pos = clusters[(int)ce.Key].vertices;
                int[] idx = clusters[(int)ce.Key].triangles;
                Vector3 p0 = pos[idx[(int)ce.Value]];
                Vector3 p1 = pos[idx[MeshUtility.Cycle3(ce.Value)]];
                edge_ht.Add(MeshUtility.Hash(new Pair<Vector3, Vector3>(p0, p1)), i);
                foreach(uint j in edge_ht[MeshUtility.Hash(new Pair<Vector3, Vector3>(p1,p0))])
                {
                    Pair<uint, uint> ce1 = ext_edges[j];
                    Vector3[] pos1 = clusters[(int)ce1.Key].vertices;
                    int[] idx1 = clusters[(int)ce1.Key].triangles;

                    if(pos1[idx1[ce1.Value]] == p1 && pos1[idx1[MeshUtility.Cycle3(ce1.Value)]] == p0)
                    {
                        edgeLink.IncreaseEdgeCost(i, j, 1);
                        edgeLink.IncreaseEdgeCost(j, i, 1);
                    }
                }
                i++;
            }
        }

        public static void BuildClusterGraph(Graph edgeLink, ref uint[] mp, int numClusters, Graph graph)
        {
            graph.Init(numClusters);
            int u = 0;
            foreach(Dictionary<uint, int> emp in edgeLink.g)
            {
                foreach(KeyValuePair<uint, int> kv in emp)
                {
                    graph.IncreaseEdgeCost(mp[u], mp[kv.Value], 1);
                }
                u++;
            }
        }
    }

    public struct ClusterGroup
    {
        static readonly int MIN_GROUP_SIZE = 8;
        /// <summary>
        /// Maximum group size
        /// </summary>
        static readonly int GROUP_SIZE = 32;

        public int mipLevel;
        public float minLODError;
        public float maxParentLODError;
        /// <summary>
        /// Subscript to cluster array
        /// </summary>
        public int[] clusters;
        public Sphere bounds;
        public Sphere lodBounds;

        /// <summary>
        /// Key: Cluster ID, Value: Edge ID
        /// </summary>
        public Pair<uint, uint>[] externalEdges;

        public static void GroupClusters(ref Cluster[] clusters, uint offset, int numClusters, ref ClusterGroup[] groups, int mipLevel)
        {
            Cluster[] clusters_view = clusters.Skip((int)offset).Take(numClusters).ToArray();

            // Take the boundary of each cluster and establish a mapping from edge id to cluster id
            uint[] mp = new uint[0]; // Edge ID to Cluster ID
            uint[] mp1 = new uint[0]; // Cluster ID to Edge ID
            Pair<uint, uint>[] extEdges = new Pair<uint, uint>[0];
            uint i = 0;
            for(uint c = offset; c < numClusters; c++)
            {
                Cluster cluster = clusters[c];
                Console.WriteLine(cluster.mipLevel == mipLevel);
                mp1.Append((uint)mp.Length);
                foreach(int e in cluster.externalEdges)
                {
                    extEdges.Append(new Pair<uint, uint>(i, (uint)e));
                    mp.Append(i);
                }
                i++;
            }

            Graph edgeLink = new Graph(), graph = new Graph();
            Cluster.BuildClustersEdgeLink(clusters_view, extEdges, edgeLink);
            Cluster.BuildClusterGraph(edgeLink, ref mp, numClusters, edgeLink);

            Partitioner partitioner = new Partitioner();
            partitioner.Partition(graph, GROUP_SIZE - 4, GROUP_SIZE);

            // TODO: Bounding Box
            foreach(Pair<int, int> kv in partitioner.ranges)
            {
                groups.Append(new ClusterGroup());
                var group = groups.Last();
                group.mipLevel = mipLevel;

                for(int ii = kv.Key; ii < kv.Value; ii++)
                {
                    uint clusterID = partitioner.nodeIDs[ii];
                    clusters[clusterID + offset].groupID = groups.Length - 1;
                    group.clusters.Append((int)(clusterID + offset));
                    for(uint e_idx = mp1[clusterID]; e_idx < mp.Length && mp[e_idx] == clusterID; e_idx++)
                    {
                        bool is_external = false;
                        foreach(KeyValuePair<uint, int> vw in edgeLink.g[(int)e_idx])
                        {
                            uint adjacentCluster = partitioner.sortTo[mp[vw.Key]];
                            if(adjacentCluster < 1 || adjacentCluster >= kv.Value)
                            {
                                is_external = true;
                                break;
                            }
                        }

                        if(is_external)
                        {
                            group.externalEdges.Append(new Pair<uint, uint>(clusterID + offset, extEdges[e_idx].Value));
                        }
                    }
                }
            }
        }

        public static void BuildParentClusters(ref ClusterGroup group, ref Cluster[] clusters)
        {
            Vector3[] pos = new Vector3[0];
            int[] idx = new int[0];
            Sphere[] lod_bounds = new Sphere[0];
            float max_parent_lod_error = 0;
            int i_ofs = 0;
            foreach(int c in group.clusters)
            {
                Cluster cluster = clusters[c];
                foreach(Vector3 p in cluster.vertices) pos.Append(p);
                foreach(int t in cluster.triangles)
                    idx.Append(t + i_ofs);
                i_ofs += cluster.vertices.Length;
                lod_bounds.Append(cluster.lodBounds);
                max_parent_lod_error = System.Math.Max(max_parent_lod_error, cluster.lodError); // Force the error of the parent node to be greater than or equal to the child node
            }
            Sphere parent_lod_bound = Sphere.FromSpheres(lod_bounds, lod_bounds.Length);

            MeshSimplifier simplifier = new MeshSimplifier(pos, pos.Length, idx, idx.Length);
            HashTable edge_ht = new HashTable((uint)group.externalEdges.Length);
            uint i = 0;
            foreach(Pair<uint, uint> kv in group.externalEdges)
            {
                Vector3[] poses = clusters[kv.Key].vertices;
                int[] idxes = clusters[kv.Key].triangles;
                Vector3 p0 = pos[idx[kv.Value]], p1 = pos[idx[MeshUtility.Cycle3(kv.Value)]];
                edge_ht.Add(MeshUtility.Hash((p0, p1)), i);
                simplifier.LockPostition(p0);
                simplifier.LockPostition(p1);
                i++;
            }

            simplifier.Simplify((Cluster.CLUSTER_SIZE - 2) * (group.clusters.Length / 2));
            Array.Resize(ref pos, simplifier.RemainingVertexCount);
            Array.Resize(ref idx, simplifier.RemainingTriangleCount * 3);

            max_parent_lod_error = System.Math.Max(max_parent_lod_error, (float)System.Math.Sqrt(simplifier.MaxError));

            Cluster.BuildAdjacencyEdgeLink(ref pos, ref idx, out Graph edgeLink);
            Cluster.BuildAdjacencyGraph(edgeLink, out Graph graph);

            Partitioner partitioner = new Partitioner();
            partitioner.Partition(graph, Cluster.CLUSTER_SIZE - 4, Cluster.CLUSTER_SIZE);

            foreach(Pair<int, int> lr in partitioner.ranges)
            {
                Cluster cluster = new Cluster();
                clusters.Append(cluster);

                Dictionary<int, int> mp = new Dictionary<int, int>();
                for(int i1 = 1; i1 < lr.Value; i1++)
                {
                    uint tIndex = partitioner.nodeIDs[i1];
                    for(int k = 0; k < 3; k++)
                    {
                        uint e_idx = (uint)(tIndex * 3 + k);
                        int v_idx = idx[e_idx];
                        if(mp[v_idx] == mp.Count)
                        {
                            // Remap vertex subsripts
                            mp[v_idx] = cluster.vertices.Length;
                            cluster.vertices.Append(pos[v_idx]);
                        }
                        bool is_external = false;
                        foreach(KeyValuePair<uint, int> adj_edge in edgeLink.g[(int)e_idx])
                        {
                            uint adj_tri = partitioner.sortTo[adj_edge.Key / 3];
                            if(adj_tri < lr.Key || adj_tri >= lr.Value)
                            {
                                // The output points are defined as boundaries in different divisions
                                is_external = true;
                                break;
                            }
                        }
                        Vector3 p0 = pos[v_idx], p1 = pos[idx[MeshUtility.Cycle3(e_idx)]];
                        if(!is_external)
                        {
                            uint pI = MeshUtility.Hash((p0, p1));
                            foreach(uint j in edge_ht[pI])
                            {
                                Pair<uint, uint> ce = group.externalEdges[j];
                                pos = clusters[ce.Key].vertices;
                                idx = clusters[ce.Key].triangles;
                                if(p0 == pos[idx[ce.Value]] && p1 == pos[idx[MeshUtility.Cycle3(ce.Value)]])
                                {
                                    is_external = true;
                                    break;
                                }
                            }
                        }

                        if(is_external)
                        {
                            cluster.externalEdges.Append(cluster.triangles.Length);
                        }
                        cluster.triangles.Append(mp[v_idx]);
                    }
                }

                cluster.mipLevel = group.mipLevel + 1;
                cluster.sphereBounds = Sphere.FromPoints(cluster.vertices, cluster.vertices.Length);
                // Force the parent node's lod bounding box to cover all child node lod bounding boxes
                cluster.lodBounds = parent_lod_bound;
                cluster.lodError = max_parent_lod_error;
                cluster.boxBounds.center = cluster.vertices[0];
                foreach(Vector3 p in cluster.vertices)
                    cluster.boxBounds += p;
            }
            group.lodBounds = parent_lod_bound;
            group.maxParentLODError = max_parent_lod_error;
        }
    }
}
