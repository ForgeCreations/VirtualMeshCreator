using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualMeshCreator.VMesh.Streaming
{
    public struct Page
    {
        public uint PartStartIndex;
        public uint PartNum;
        public uint NumClusters;
    }

    public static class MeshEncode
    {
        public static void RemoveRootPagesFromRange(uint StartPage, uint NumPages, uint NumResourceRootPages)
        {
            if(StartPage < NumResourceRootPages)
            {
                NumPages = (uint)System.Math.Max((int)NumPages - (int)(NumResourceRootPages - StartPage), 0);
                StartPage = NumResourceRootPages;
            }

            if(NumPages == 0)
            {
                StartPage = 0;
            }
        }

        public static void RemovePageFromRange(uint StartPage, uint NumPages, uint PageIndex)
        {
            if(NumPages > 0)
            {
                if(StartPage == PageIndex)
                {
                    StartPage++;
                    NumPages--;
                }

                else if(StartPage + NumPages - 1 == PageIndex)
                {
                    NumPages--;
                }
            }

            if(NumPages == 0)
            {
                StartPage = 0;
            }
        }
    }
}
