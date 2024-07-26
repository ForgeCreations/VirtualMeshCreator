using VirtualMeshCreator.Math;

namespace VirtualMeshCreator.VMesh.Encoding
{
    public struct PageSections
    {
        public uint Cluster;
        //public uint MaterialTable;
        public uint VertReuseBatchInfo;
        public uint DecodeInfo;
        public uint Index;
        public uint Position;
        public uint Attributes;
    }

    public struct Page
    {
        public uint PartStartIndex;
        public uint PartNum;
        public uint NumClusters;
        public PageSections GPUSizes;
    }

    public struct UVRange
    {
        public Vector2 Min;
        public Vector2 GapStart;
        public Vector2 GapLength;
        public int Precision;
        public int Pad;
    }

    public struct EncodingInfo
    {
        public uint BitsPerIndex;
        public uint BitsPerAttribute;
        public uint UVPrec;
        public uint ColorMode;
        public Vector4 ColorMin;
        public Vector4 ColorBits;
        public PageSections GPUSizes;
        public UVRange[] UVRanges;
    }

    public static class VMeshEncode
    {
        public static uint Align(uint value, uint alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

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
