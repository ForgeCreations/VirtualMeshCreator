using ObjLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VirtualMeshCreator.Math;
using VirtualMeshCreator.Utility;

namespace VirtualMeshCreator.VMesh
{
    public struct VirtualMesh
    {
        public Cluster[] clusters;
        public ClusterGroup[] groups;
        public int numMipLevels;

        public void Build(ref Mesh mesh)
        {
            Console.WriteLine("\n Begin Building Virtual Mesh\n\n");

            Vector3[] pos = mesh.vertices;
            int[] idx = mesh.triangles;

            Console.WriteLine("Fixing up Mesh");

            //Use a simplifier and set a target greater than the number of triangles to remove duplicate vertices and triangles
            MeshSimplifier simplifier = new MeshSimplifier(pos, pos.Length, idx, idx.Length);
            simplifier.Simplify(idx.Length);
            Array.Resize(ref pos, simplifier.RemainingVertexCount);
            Array.Resize(ref idx, simplifier.RemainingTriangleCount * 3);
            Console.WriteLine($"Num Vertices: {pos.Length}, Num Triangles: {idx.Length / 3}\n\n");

            Console.WriteLine("Clustering Triangles");

            //Create clusters from triangles
            Cluster.ClusterTriangles(ref pos, ref idx, ref clusters);

            int levelOffset = 0, mipLevel = 0;

            Console.WriteLine("Begin Building DAG Tree\n");
            while(true)
            {
                Console.Write($"### Level {mipLevel} ###\n");
                Console.Write($"Num Clusters: {clusters.Length - levelOffset}\n");
                LogClusterSize(ref clusters, levelOffset, clusters.Length);
                
                int numLevelClusters = clusters.Length - levelOffset;
                if(numLevelClusters <= 1)
                    break;

                int prevClusterNum = clusters.Length;
                int preGroupNum = groups.Length;

                //Group Clusters
                Console.WriteLine("Grouping Clusters");
                ClusterGroup.GroupClusters(ref clusters, (uint)levelOffset, numLevelClusters, ref groups, mipLevel);
                Console.WriteLine($"Num Groups: {groups.Length - preGroupNum}\n");
                LogGroupSize(ref groups, preGroupNum, groups.Length);

                //Merge and Simplify clusters within groups to generate upper-level clusters
                Console.WriteLine("Building Parent Clusters: ");
                for(int i = preGroupNum; i < groups.Length; i++)
                {
                    ClusterGroup.BuildParentClusters(ref groups[i], ref clusters);
                }

                levelOffset = prevClusterNum;
                mipLevel++;

                Console.WriteLine("\n");
            }
            numMipLevels = mipLevel + 1;

            Console.WriteLine("End Building DAG Tree\n");
            Console.WriteLine($"Total Clusters: {clusters.Length}\n\n");

            Console.WriteLine("# End Building Virtual Mesh\n\n");
        }

        private Heap FindDAGCut(Cluster[] clusters, ClusterGroup[] groups, uint targetNumTris, float targetError, uint targetOvershoot)
        {
            ClusterGroup RootGroup = groups.Last();
            Cluster RootCluster = clusters[RootGroup.clusters[0]];
            bool hitTargetBefore = false;

            float MinError = RootCluster.lodError;

            Heap heap = new Heap();
            heap.Add(-RootCluster.lodError, (uint)RootGroup.clusters[0]);

            while(true)
            {
                //Grab highest error cluster to replace to reduce cut error
                Cluster cluster = clusters[heap.Top()];

                if(cluster.mipLevel == 0)
                    break;
                if(cluster.generatingGroupID == int.MaxValue)
                    break;

                bool bHitTarget = heap.Num() * Cluster.CLUSTER_SIZE > targetNumTris || MinError < targetError;

                //Overshoot the target by TargetOvershoot number of triangles. This allows granular edge collapses to better minimize error to the target.
                if(targetOvershoot > 0 && bHitTarget && !hitTargetBefore)
                {
                    targetNumTris = heap.Num() * (uint)Cluster.CLUSTER_SIZE + targetOvershoot;
                    bHitTarget = false;
                    hitTargetBefore = true;
                }

                if(bHitTarget && cluster.lodError < MinError)
                    break;

                heap.Pop();

                //check(Cluster.LODError <= MinError);
                //Console.WriteLine(cluster.lodError <= MinError);
                MinError = cluster.lodError;

                foreach(uint Child in groups[cluster.generatingGroupID].clusters)
                {
                    if(!heap.IsPresent(Child))
                    {
                        Cluster ChildCluster = clusters[Child];

                        //check(ChildCluster.MipLevel < cluster.mipLevel);
                        //Console.WriteLine(ChildCluster.mipLevel < cluster.mipLevel);
                        //check(ChildCluster.LODError <= MinError);
                        //Console.WriteLine(ChildCluster.lodError <= MinError);
                        heap.Add(-ChildCluster.lodError, Child);
                    }
                }
            }

            return heap;
        }

        public void Save(string fileName, string exportPath)
        {
            const string FILE_EXTENSION = ".vmesh";

            FileStream file = new FileStream(exportPath + "/" + fileName + FILE_EXTENSION, FileMode.Create);
            using(BinaryWriter writer = new BinaryWriter(file))
            {
                writer.Write(clusters.Length); //Num Clusters
                writer.Write(groups.Length); //Num Groups
                writer.Write(0); //group data ofs
                writer.Write(0);
                foreach(Cluster cluster in clusters)
                {
                    writer.Write(cluster.vertices.Length); //Num Verticies
                    writer.Write(0); //v data ofs
                    writer.Write(cluster.triangles.Length / 3); //Num Triangles
                    writer.Write(0); //t data ofs

                    //Bounds
                    writer.Write((uint)cluster.sphereBounds.center.x);
                    writer.Write((uint)cluster.sphereBounds.center.y);
                    writer.Write((uint)cluster.sphereBounds.center.z);
                    writer.Write((uint)cluster.sphereBounds.radius);

                    //Lod Bounds
                    writer.Write((uint)cluster.lodBounds.center.x);
                    writer.Write((uint)cluster.lodBounds.center.y);
                    writer.Write((uint)cluster.lodBounds.center.z);
                    writer.Write((uint)cluster.lodBounds.radius);

                    //Parent LOD Bounds
                    Sphere parentLodBounds = groups[cluster.groupID].lodBounds;
                    float max_parent_lod_error = groups[cluster.groupID].maxParentLODError;
                    writer.Write((uint)parentLodBounds.center.x);
                    writer.Write((uint)parentLodBounds.center.y);
                    writer.Write((uint)parentLodBounds.center.z);
                    writer.Write((uint)parentLodBounds.radius);

                    writer.Write((uint)cluster.lodError);
                    writer.Write((uint)max_parent_lod_error);
                    writer.Write(cluster.groupID);
                    writer.Write(cluster.mipLevel);
                }

                //packed_data[2] = packed_data.size(); //group data ofs
                writer.Write(0);
                writer.Write(0);
                foreach(ClusterGroup group in groups)
                {
                    writer.Write(group.clusters.Length); //num cluster
                    writer.Write(0); //cluter data ofs
                    writer.Write((uint)group.maxParentLODError);
                    writer.Write(0);

                    //Lod Bounds
                    writer.Write((uint)group.lodBounds.center.x);
                    writer.Write((uint)group.lodBounds.center.y);
                    writer.Write((uint)group.lodBounds.center.z);
                    writer.Write((uint)group.lodBounds.radius);
                }
                int i = 0;
                foreach(Cluster cluster in clusters)
                {
                    int ofs = 4 + 20 * i;
                    //packed_data[ofs + 1] = packed_data.size();
                    foreach(Vector3 p in cluster.vertices)
                    {
                        writer.Write((uint)p.x);
                        writer.Write((uint)p.y);
                        writer.Write((uint)p.z);
                    }

                    //packed_data[ofs + 3] = packed_data.size();
                    for(int t = 0; t < cluster.triangles.Length / 3; t++)
                    {
                        //Triangle Data
                        int i0 = cluster.triangles[t * 3];
                        int i1 = cluster.triangles[t * 3 + 1];
                        int i2 = cluster.triangles[t * 3 + 2];
                        //assert(i0 < 256 && i1 < 256 && i2 < 256);
                        //Console.WriteLine(i0 < 256 && i1 < 256 && i2 < 256);

                        int packed_tri = i0 | (i1 << 8) | (i2 << 16);
                        writer.Write(packed_tri);
                    }
                    i++;
                }
                i = 0;
                writer.Close();
            }
            file.Close();
        }

        #region Debug
        public void LogClusterSize(ref Cluster[] clusters, int begin, int end)
        {
            float maxsz = 0.0f, minsz = 100000.0f, avgsz = 0.0f;
            for(int i = begin; i < end; i++)
            {
                Cluster cluster = clusters[i];
                //assert(cluster.verts.size() < 256);
                Console.WriteLine("Cluster Vertex Count Exceeds Limit: " + (cluster.vertices.Length < 256));
                float sz = cluster.triangles.Length / 3.0f;
                if(sz > maxsz) maxsz = sz;
                if(sz < minsz) minsz = sz;
                avgsz += sz;
            }
            avgsz /= end - begin;
            Console.WriteLine($"Cluster Size: Min = {minsz}, Max = {maxsz}, Average = {avgsz}\n");
        }

        public void LogGroupSize(ref ClusterGroup[] groups, int begin, int end)
        {
            float maxsz = 0.0f, minsz = 100000.0f, avgsz = 0.0f;
            for(int i = begin; i < end; i++)
            {
                float sz = groups[i].clusters.Length;
                if(sz > maxsz) maxsz = sz;
                if(sz < minsz) minsz = sz;
                avgsz += sz;
            }
            avgsz /= end - begin;
            Console.WriteLine($"Group Size: Min = {minsz}, Max = {maxsz}, Average = {avgsz}\n");
        }
        #endregion
    }
}
