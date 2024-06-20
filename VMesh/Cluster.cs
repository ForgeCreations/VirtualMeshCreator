using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualMeshCreator.Math;
using VirtualMeshCreator.Utility;

namespace VirtualMeshCreator.VMesh
{
    public struct Cluster
    {
        public static readonly int CLUSTER_SIZE = 128;

        public int groupID;
        public int mipLevel;
        public float lodError;
        public Bounds boxBounds;
        public Sphere sphereBounds;
        public Sphere lodBounds;

        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
        public int[] externalEdges;

        //Edge Hashing, finds the opposite edges that share a vertex, indicating that two triangles are adjacent
        public static void BuildAdjacencyEdgeLink(ref Vector3[] verticies, ref int[] triangles, out Graph edgeLink)
        {
            edgeLink = new Graph(triangles.Length);
        }

        //Contruct a triangle adjacency graph based on the edge adjacency. The edge weight is 1. When local needs to be added, the adjacency edge needs to be large enough.
        public static void BuildAdjacencyGraph(ref Graph edgeLink, out Graph graph)
        {
            graph = new Graph(edgeLink.g.Length / 3);
            foreach(Dictionary<int, int> emp in edgeLink.g)
            {
                foreach(KeyValuePair<int, int> kv in emp)
                {
                    graph.IncreaseEdgeCost(kv.Key / 3, kv.Value / 3, 1);
                }
            }
        }

        public static void ClusterTriangles(ref Vector3[] verticies, ref int[] triangles, ref Cluster[] clusters)
        {
            BuildAdjacencyEdgeLink(ref verticies, ref triangles, out Graph edgeLink);
            BuildAdjacencyGraph(ref edgeLink, out Graph graph);

            Partitioner partitioner = new Partitioner(graph.g.Length);
            partitioner.Partition(ref graph, CLUSTER_SIZE - 4, CLUSTER_SIZE);
        }

        public static void BuildClusterGraph(ref Graph edgeLink, ref int[] mp, int numClusters, out Graph graph)
        {
            graph = new Graph(numClusters);
            foreach(Dictionary<int, int> emp in edgeLink.g)
            {
                foreach(KeyValuePair<int, int> kv in emp)
                {
                    graph.IncreaseEdgeCost(mp[kv.Key], mp[kv.Value], 1);
                }
            }
        }
    }

    public struct ClusterGroup
    {
        //static readonly int MIN_GROUP_SIZE = 8;
        static readonly int GROUP_SIZE = 32;

        public int mipLevel;
        public float minLODError;
        public float maxParentLODError;
        public int[] clusters; //Subscript to cluster array
        public Sphere bounds;
        public Sphere lodBounds;

        //Key: Cluster ID, Value: Edge ID
        public Pair<uint, uint>[] externalEdges;

        public static void GroupClusters(ref Cluster[] clusters, uint offset, int numClusters, ref ClusterGroup[] groups, int mipLevel)
        {
            //Take the boundary of each cluster and establish a mapping from edge id to cluster id
            int[] mp = new int[0]; //Edge ID to Cluster ID
            int[] mp1 = new int[0]; //Cluster ID to Edge ID
            Dictionary<int, int>[] extEdges = new Dictionary<int, int>[0];

            Graph edgeLink = new Graph();
            Cluster.BuildClusterGraph(ref edgeLink, ref mp, numClusters, out Graph graph);

            Partitioner partitioner = new Partitioner(graph.g.Length);
            partitioner.Partition(ref graph, GROUP_SIZE - 4, GROUP_SIZE);

            //TODO: Bounding Box
            foreach(Pair<int, int> kv in partitioner.ranges)
            {
                groups.ToList().Add(new ClusterGroup());
                var group = groups.Last();
                group.mipLevel = mipLevel;

                for(int i = kv.Key; i < kv.Value; i++)
                {
                    uint clusterID = (uint)partitioner.nodeIDs[i];
                    clusters[clusterID + offset].groupID = groups.Length - 1;
                    group.clusters.ToList().Add((int)(clusterID + offset));
                    for(int e_idx = mp1[clusterID]; e_idx < mp.Length && mp[e_idx] == clusterID; e_idx++)
                    {
                        bool is_external = false;
                        foreach(KeyValuePair<int, int> vw in edgeLink.g[e_idx])
                        {
                            int adjacentCluster = partitioner.sortTo[mp[vw.Key]];
                            if(adjacentCluster < 1 || adjacentCluster >= kv.Value)
                            {
                                is_external = true;
                                break;
                            }
                        }

                        if(is_external)
                        {
                            uint e = (uint)extEdges[e_idx][1];
                            group.externalEdges.ToList().Add(new Pair<uint, uint>(clusterID + offset, e));
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
                foreach(Vector3 p in cluster.vertices) pos.ToList().Add(p);
                foreach(int t in cluster.triangles)
                    idx.ToList().Add(t + i_ofs);
                i_ofs += cluster.vertices.Length;
                lod_bounds.ToList().Add(cluster.lodBounds);
                max_parent_lod_error = System.Math.Max(max_parent_lod_error, cluster.lodError); //Force the error of the parent node to be greater than or equal to the child node
            }
            Sphere parent_lod_bound = Sphere.FromSpheres(lod_bounds, lod_bounds.Length);

            MeshSimplifier simplifier = new MeshSimplifier(pos, pos.Length, idx, idx.Length);
            HashTable edge_ht = new HashTable(group.externalEdges.Length);
            uint i = 0;

            foreach(Pair<uint, uint> kv in group.externalEdges)
            {
                Vector3[] poses = clusters[kv.Key].vertices;
                int[] idxes = clusters[kv.Key].triangles;
                Vector3 p0 = pos[idx[kv.Value]], p1 = pos[idx[MeshUtility.cycle3(kv.Value)]];
                edge_ht.Add(MeshUtility.hash(new KeyValuePair<Vector3, Vector3>(p0, p1)), i);
                simplifier.LockPostition(p0);
                simplifier.LockPostition(p1);
                i++;
            }

            simplifier.Simplify((Cluster.CLUSTER_SIZE - 2) * (group.clusters.Length / 2));
            Array.Resize(ref pos, simplifier.RemainingVertexCount);
            Array.Resize(ref idx, simplifier.RemainingTriangleCount * 3);

            max_parent_lod_error = System.Math.Max(max_parent_lod_error, (float)System.Math.Sqrt(simplifier.MaxError));

            Cluster.BuildAdjacencyEdgeLink(ref pos, ref idx, out Graph edge_link);
            Cluster.BuildAdjacencyGraph(ref edge_link, out Graph graph);

            Partitioner partitioner = new Partitioner(graph.g.Length);
            partitioner.Partition(ref graph, Cluster.CLUSTER_SIZE - 4, Cluster.CLUSTER_SIZE);

            foreach(Pair<int, int> lr in partitioner.ranges)
            {
                Cluster cluster = new Cluster();
                clusters.ToList().Add(cluster);

                Dictionary<int, int> mp = new Dictionary<int, int>();
                for(int i1 = 1; i1 < lr.Value; i1++)
                {
                    int tIndex = partitioner.nodeIDs[i1];
                    for(int k = 0; k < 3; k++)
                    {
                        uint e_idx = (uint)(tIndex * 3 + k);
                        int v_idx = idx[e_idx];
                        if(mp[v_idx] == mp.Count)
                        {
                            //Remap vertex subsripts
                            mp[v_idx] = cluster.vertices.Length;
                            cluster.vertices.ToList().Add(pos[v_idx]);
                        }
                        bool is_external = false;
                        foreach(KeyValuePair<int, int> adj_edge in edge_link.g[e_idx])
                        {
                            int adj_tri = partitioner.sortTo[adj_edge.Key / 3];
                            if(adj_tri < lr.Key || adj_tri >= lr.Value)
                            {
                                //The output points are defined as boundaries in different divisions
                                is_external = true;
                                break;
                            }
                        }
                        Vector3 p0 = pos[v_idx], p1 = pos[idx[MeshUtility.cycle3(e_idx)]];
                        if(!is_external)
                        {
                            uint pI = MeshUtility.hash(new KeyValuePair<Vector3, Vector3>(p0, p1));
                            for(uint j = edge_ht.First(pI); edge_ht.IsValid(j); edge_ht.Next(j))
                            {
                                Pair<uint, uint> ce = group.externalEdges[j];
                                pos = clusters[ce.Key].vertices;
                                idx = clusters[ce.Key].triangles;
                                if(p0 == pos[idx[ce.Value]] && p1 == pos[idx[MeshUtility.cycle3(ce.Value)]])
                                {
                                    is_external = true;
                                    break;
                                }
                            }
                        }

                        if(is_external)
                        {
                            cluster.externalEdges.ToList().Add(cluster.triangles.Length);
                        }
                        cluster.triangles.ToList().Add(mp[v_idx]);
                    }
                }

                cluster.mipLevel = group.mipLevel + 1;
                cluster.sphereBounds = Sphere.FromPoints(cluster.vertices, cluster.vertices.Length);
                //Force the parent node's lod bounding box to cover all child node lod bounding boxes
                cluster.lodBounds = parent_lod_bound;
                cluster.lodError = max_parent_lod_error;
                cluster.boxBounds.center = cluster.vertices[0];
                foreach(Vector3 p in cluster.vertices)
                    cluster.boxBounds = cluster.boxBounds + p;
            }
            group.lodBounds = parent_lod_bound;
            group.maxParentLODError = max_parent_lod_error;
        }
    }
}
