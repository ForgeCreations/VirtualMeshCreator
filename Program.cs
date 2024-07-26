using ShellProgressBar;
using System;
using VirtualMeshCreator.VMesh;

namespace VirtualMeshCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "VirtualMeshCreator";
            Mesh mesh = new Mesh("ObjModel");
            Console.WriteLine("Enter path to obj model");
            bool loaded = mesh.LoadModel3(Console.ReadLine());
            if(loaded)
            {
                VirtualMesh vMesh = new VirtualMesh();
                vMesh.Build(ref mesh);
            }
        }
    }
}
