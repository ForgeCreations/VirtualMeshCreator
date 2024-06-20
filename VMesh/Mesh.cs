using ObjLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualMeshCreator.Math;

namespace VirtualMeshCreator.VMesh
{
    public struct Mesh
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
        public Vector3[] normals;

        public Mesh(string name)
        {
            vertices = new Vector3[0];
            triangles = new int[0];
            uvs = new Vector2[0];
            normals = new Vector3[0];
        }

        /// <summary>
        /// Load an ".obj" model from the path
        /// </summary>
        /// <param name="path">The path to the model</param>
        public void LoadModel(string path)
        {
            try
            {
                using(StreamReader sr = new StreamReader(path))
                {
                    string line = sr.ReadLine();
                    Console.WriteLine("Started Loading Mesh");
                    Console.WriteLine(line);
                    while((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();
                        string[] parts = line.Split(' ');
                        //Verticies
                        if(parts[0] == "v")
                        {
                            float x = float.Parse(parts[1]);
                            float y = float.Parse(parts[2]);
                            float z = float.Parse(parts[3]);
                            vertices.ToList().Add(new Vector3(x, y, z));
                        }

                        //Triangles
                        else if(parts[0] == "f")
                        {
                            for(int i = 1; i < 4; i++)
                            {
                                string[] vertexIndices = parts[i].Split('/');
                                int index = int.Parse(vertexIndices[0]) - 1;
                                triangles.ToList().Add(index);
                            }
                        }

                        //UVs
                        else if(parts[0] == "vt")
                        {
                            float u = float.Parse(parts[1]);
                            float v = float.Parse(parts[2]);
                            uvs.ToList().Add(new Vector2(u, v));
                        }
                    }
                }
            }

            catch(Exception ex)
            {
                Console.WriteLine("Error loading OBJ file: " + ex.Message);
            }
        }

        /// <summary>
        /// Load an ".obj" model from the path
        /// </summary>
        /// <param name="path">The path to the model</param>
        public void LoadModel2(string path)
        {
            StreamReader sr = new StreamReader(path);
            string line = sr.ReadLine();
            Console.WriteLine("Started Loading");
            Console.WriteLine(line);
            while(line != null)
            {
                line = line.Trim();
                string[] parts = line.Split(' ');
                
                //Verticies
                if(parts[0] == "v")
                {
                    float x = float.Parse(parts[1]);
                    float y = float.Parse(parts[2]);
                    float z = float.Parse(parts[3]);
                    vertices.ToList().Add(new Vector3(x, y, z));
                    Console.WriteLine("Added Vertex");
                }
                
                //Triangles
                else if(parts[0] == "f")
                {
                    for(int i = 1; i < 4; i++)
                    {
                        string[] vertexIndices = parts[i].Split('/');
                        int index = int.Parse(vertexIndices[0]) - 1;
                        triangles.ToList().Add(index);
                        Console.WriteLine("Added Triangle");
                    }
                }
                
                //UVs
                else if(parts[0] == "vt")
                {
                    float u = float.Parse(parts[1]);
                    float v = float.Parse(parts[2]);
                    uvs.ToList().Add(new Vector2(u, v));
                    Console.WriteLine("Added UV Coordinate");
                }
            }
        }
    
        public bool LoadModel3(string path)
        {
            var objHandle = ObjFileLoader.CreateHandle();
            bool loaded = ObjFileLoader.Load(objHandle, path);
            vertices = new Vector3[objHandle.Vertices.Length];
            uvs = new Vector2[objHandle.Vertices.Length];
            for(int v = 0; v < objHandle.Vertices.Length; v++)
            {
                vertices[v] = new Vector3((float)objHandle.Vertices[v].Position.X, (float)objHandle.Vertices[v].Position.Y, (float)objHandle.Vertices[v].Position.Z);
                uvs[v] = new Vector2((float)objHandle.Vertices[v].UV.X, (float)objHandle.Vertices[v].UV.Y);
            }
            triangles = objHandle.Indices;
            return loaded;
        }
    }
}
