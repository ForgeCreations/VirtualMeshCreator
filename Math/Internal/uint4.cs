using System;
using System.Runtime.InteropServices;

namespace VirtualMeshCreator.Math.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    public struct uint4
    {
        public uint x, y, z, w;

        public uint4(uint x, uint y, uint z, uint w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public uint this[int index]
        {
            get
            {
                switch(index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;
                    default: throw new IndexOutOfRangeException("Invalid index");
                }
            }

            set
            {
                switch(index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    case 3: w = value; break;
                    default: throw new IndexOutOfRangeException("Invalid index");
                }
            }
        }

        public uint this[uint index]
        {
            get
            {
                switch(index)
                {
                    case 0u: return x;
                    case 1u: return y;
                    case 2u: return z;
                    case 3u: return w;
                    default: throw new IndexOutOfRangeException("Invalid index");
                }
            }

            set
            {
                switch(index)
                {
                    case 0u: x = value; break;
                    case 1u: y = value; break;
                    case 2u: z = value; break;
                    case 3u: w = value; break;
                    default: throw new IndexOutOfRangeException("Invalid index");
                }
            }
        }
    }
}
