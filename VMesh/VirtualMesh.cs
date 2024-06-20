using System;
using System.IO;
using System.Threading.Tasks;
using ShellProgressBar;
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
            Console.WriteLine("Num Vertices: {}, Num Triangles: {}\n\n", pos.Length, idx.Length / 3);

            Console.WriteLine("Clustering Triangles");

            //Create clusters from triangles
            Cluster.ClusterTriangles(ref pos, ref idx, ref clusters);

            int levelOffset = 0, mipLevel = 0;

            Console.WriteLine("Begin Building DAG Tree\n");
            while(true)
            {
                Console.Write("### Level {} ###\n", mipLevel);
                Console.Write("Num Clusters: {}\n", clusters.Length - levelOffset);
                LogClusterSize(ref clusters, levelOffset, clusters.Length);
                
                int numLevelClusters = clusters.Length - levelOffset;
                if(numLevelClusters <= 1)
                    break;

                int prevClusterNum = clusters.Length;
                int preGroupNum = groups.Length;

                //Group Clusters
                Console.WriteLine("Grouping Clusters");
                using(ProgressBar groupingProgress = new ProgressBar(1920, "Grouping Clusters", ConsoleColor.Cyan))
                    ClusterGroup.GroupClusters(ref clusters, (uint)levelOffset, numLevelClusters, ref groups, mipLevel);
                Console.WriteLine("Num Groups: {}\n", groups.Length - preGroupNum);
                LogGroupSize(ref groups, preGroupNum, groups.Length);

                //Merge and Simplify clusters within groups to generate upper-level clusters
                Console.WriteLine("Building Parent Clusters: ");
                for(int i = preGroupNum; i < groups.Length; i++)
                {
                    ClusterGroup.BuildParentClusters(ref groups[i], ref clusters);
                }

                levelOffset = prevClusterNum;
                mipLevel++;

                //Console.WriteLine("\n");
            }
            numMipLevels = mipLevel + 1;

            Console.WriteLine("End Building DAG Tree\n");
            Console.WriteLine("Total Clusters: {}\n\n", clusters.Length);

            Console.WriteLine("# End Building Virtual Mesh\n\n");
        }

        public void Save(string fileName, string exportPath)
        {
            const string FILE_EXTENSION = ".vmesh";

            FileStream file = new FileStream(exportPath + "/" + fileName + FILE_EXTENSION, FileMode.Create);
            using(ProgressBar saveProgress = new ProgressBar(1920, "Saving Virtual Mesh File...", ConsoleColor.Cyan))
            {
                using(StreamWriter writer = new StreamWriter(file))
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

                            int packed_tri = i0 | (i1 << 8) | (i2 << 16);
                            writer.Write(packed_tri);
                        }
                        i++;
                    }
                    i = 0;
                    writer.Close();
                }
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
                float sz = cluster.triangles.Length / 3.0f;
                if(sz > maxsz) maxsz = sz;
                if(sz < minsz) minsz = sz;
                avgsz += sz;
            }
            avgsz /= end - begin;
            Console.WriteLine("Cluster Size: Min = {}, Max = {}, Average = {}\n", minsz, maxsz, avgsz);
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
            Console.WriteLine("Group Size: Min = {}, Max = {}, Average = {}\n", minsz, maxsz, avgsz);
        }
        #endregion
    }
}
