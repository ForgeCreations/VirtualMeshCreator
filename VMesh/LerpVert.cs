using System;
using System.Collections.Generic;
using System.Linq;
using VirtualMeshCreator.Math;

namespace VirtualMeshCreator.VMesh
{
    public struct LerpVert
    {
        public Vector3 Position;

        public Vector3 TangentX;
        public Vector3 TangentY;
        public Vector3 TangentZ;

        public Color Color;
        public Vector2[] UVs;

        public static LerpVert operator *(LerpVert vert, float a)
        {
            LerpVert v = new LerpVert
            {
                Position = vert.Position * a,
                TangentX = vert.TangentX * a,
                TangentY = vert.TangentY * a,
                TangentZ = vert.TangentZ * a,
                Color = vert.Color * a
            };

            return v;
        }
    }
}
