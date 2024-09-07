using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VirtualMeshCreator.VMesh.Streaming
{
    [StructLayout(LayoutKind.Sequential)]
    public struct StreamingRequest
    {
        public uint[] PageIndexes;
    }
}
