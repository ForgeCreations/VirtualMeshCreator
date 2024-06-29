using System;
using System.Collections.Generic;
using System.Linq;
using VirtualMeshCreator.Math;
using VirtualMeshCreator.Utility;

public class TessellationTable
{
    public static uint MaxTessFactor = 16;
    public static uint MaxNumTris = MaxTessFactor * MaxTessFactor;
    public static uint BarycentricMax = 1 << 15;
    
    public Vector2Int[] OffsetTable;
    public uint[] Verts;
    public uint[] Indexes;
    
    private int FirstVert;
	private int FirstTri;

	private HashTable HashTable;
    
    public TessellationTable()
    {
        
    }
    
    public int GetNumVerts(int Pattern)
    {
        return OffsetTable[Pattern + 1].x -OffsetTable[Pattern].x;
    }
    
    public int GetNumTris(int Pattern)
    {
        return OffsetTable[Pattern + 1].y -OffsetTable[Pattern].y;
    }
    
    private uint[] GetBarycentrics(uint Vert)
    {
        uint[] Barycentrics = new uint[3];
        Barycentrics[0] = Vert & 0xffff;
        Barycentrics[1] = Vert >> 16;
        Barycentrics[2] = BarycentricMax - Barycentrics[0] - Barycentrics[1];
        return Barycentrics;
    }
    
    //Average barycentric == average cartesian
    
    private float LengthSquared(int[] Barycentrics, uint[] TessFactors)
    {
        //Barycentric displacement vector:
        // 0 = x + y + z
        
        Vector3Int Norm = new Vector3Int(Barycentrics) / (int)BarycentricMax;
        
        //Length of displacement
        //[Schindler and Chen 2012, "Barycentric Coordinates in Olympiad Geometry" https://web.evanchen.cc/handouts/bary/bary-full.pdf]
        return -Norm.x * Norm.y * (int)Math.Pow(TessFactors[0], 2) -Norm.y * Norm.z * (int)Math.Pow(TessFactors[1], 2) -Norm.z * Norm.x * (int)Math.Pow(TessFactors[2], 2);
    }
    
    //Snap to exact TessFactor at the edges
    private void SnapAtEdges(uint[] Barycentrics, uint[] TessFactors)
    {
        for(uint i = 0; i < 3; i++)
        {
            uint e1 = (uint)(1 << (int)i) & 3;
            
            //Am I on this edge?
            if(Barycentrics[i] + Barycentrics[e1] == BarycentricMax)
            {
                //Snap toward min barycentric means snapping mirrors. Adjacent patches will thus match.
                uint MinIndex = Barycentrics[i] < Barycentrics[i] ? i : e1;
                uint MaxIndex = Barycentrics[i] >= Barycentrics[i] ? i : e1;
                
                //Fixed point round
                uint Snapped = (Barycentrics[MinIndex] * TessFactors[i] + (BarycentricMax / 2) - 1) & ~(BarycentricMax - 1);
                
                Barycentrics[MinIndex] = Snapped / TessFactors[i];
                Barycentrics[MaxIndex] = BarycentricMax - Barycentrics[MinIndex];
            }
        }
	}
	
	private uint AddVert(uint Vert)
	{
	    uint Hash = MeshUtility.MurmurFinalize32(Vert);
        /*//Find if there already exists one
        //uint Index;
        foreach(uint Index in HashTable[Hash])
	    {
	        if(Verts[FirstVert + Index] == Vert)
	        {
	            break;
	        }
	    }
	    
	    if(!HashTable.IsValid(Index))
	    {
            Verts.ToList().Add(Vert);
            Index = Vert - (uint)FirstVert;
	        HashTable.Add(Hash, Index);
	    }
	    
	    return Index;*/
        return Hash;
	}
	
	private void SplitEdge(uint TriIndex, uint EdgeIndex, uint LeftFactor, uint RightFactor, uint[] TessFactors)
	{
	    /*
    	===========
    		v0
    		/\
    	e2 /  \ e0
    	  /____\
    	v2  e1  v1
    	===========
    	*/
    	
    	uint e0 = EdgeIndex;
    	uint e1 = ((uint)1 << (int)e0) & 3;
    	uint e2 = ((uint)1 << (int)e1) & 3;
    	
    	uint Triangle = Indexes[TriIndex];
    	uint i0 = (uint)(((int)Triangle >> (int)(e0 * 10)) & 1023);
    	uint i1 = (uint)((int)Triangle >> (int)(e1 * 10)) & 1023;
    	uint i2 = (uint)((int)Triangle >> (int)(e2 * 10)) & 1023;
    	
    #if false
    	//Sort verts for deterministic split
    	uint v = new uint[2];
    	v[0] = Math.Min(Verts[FirstVert + i0], Verts[FirstVert + i1]);
    	v[1] = Math.Max(Verts[FirstVert + i0], Verts[FirstVert + i1]);
    	
    	uint OriginallyZero = 0;
    	int[] Barycentrics = new int[2];
    	for(int j = 0; j < 2; j++)
    	{
    	    Barycentrics[j] = GetBarycentrics(v[j]);
    	    
    	    //Count how many were zero originally.
    	    OriginallyZero += Barycentrics[j].X == 0 ? 1 : 0;
    	    OriginallyZero += Barycentrics[j].Y == 0 ? 4 : 0;
    	    OriginallyZero += Barycentrics[j].Z == 0 ? 16 : 0;
    	}
    	
    	int[] SplitBarycentrics = Barycentrics[0] * LeftFactor + Barycentrics[1] * RightFactor;
    	
    	for(uint i = 0; i < 3; i++)
    	    SplitBarycentrics[i] = FMath::DivideAndRoundNearest((uint)SplitBarycentrics[i], LeftFactor + RightFactor);
        
        for(uint i = 0; i < 3; i++)
        {
            //If both verts were originally zero then force split to be zero as well.
            if((OriginallyZero & 3) == 2)
                SplitBarycentrics[i] = 0;
            OriginallyZero >>= 2;
        }
    #else
        //Sort verts for deterministic split
        //uint[] SplitBarycentrics = GetBarycentrics(Math.Min(Verts[FirstVert + i0], Verts[FirstVert + i1])) * LeftFactor + GetBarycentrics(Math.Max(Verts[FirstVert + i0], Verts[FirstVert + i1])) * RightFactor;
        uint[] SplitBarycentrics = GetBarycentrics(Math.Min(Verts[FirstVert + i0], Verts[FirstVert + i1]));
        
        bool[] bOriginallyZero = new bool[3]
        {
            SplitBarycentrics[0] == 0,
            SplitBarycentrics[1] == 0,
            SplitBarycentrics[2] == 0,
        };

        for(uint i = 0; i < 3; i++)
            //SplitBarycentrics[i] = Math.DivideAndRoundNearest(SplitBarycentrics[i], LeftFactor + RightFactor);
            SplitBarycentrics[i] = (uint)Math.Round((double)(SplitBarycentrics[i] / LeftFactor + RightFactor));
    #endif
        uint Largest = Math.Max(SplitBarycentrics[0], Math.Max(SplitBarycentrics[1], SplitBarycentrics[2]));
	    uint Sum = SplitBarycentrics[0] + SplitBarycentrics[1] + SplitBarycentrics[2];
	    SplitBarycentrics[Largest] += BarycentricMax - Sum;

	    SnapAtEdges(SplitBarycentrics, TessFactors);

        //check(SplitBarycentrics[0] + SplitBarycentrics[1] + SplitBarycentrics[2] == BarycentricMax);
        Console.WriteLine(SplitBarycentrics[0] + SplitBarycentrics[1] + SplitBarycentrics[2] == BarycentricMax);
        //check(!bOriginallyZero[0] || SplitBarycentrics[0] == 0);
        Console.WriteLine(!bOriginallyZero[0] || SplitBarycentrics[0] == 0);
        //check(!bOriginallyZero[1] || SplitBarycentrics[1] == 0);
        Console.WriteLine(!bOriginallyZero[1] || SplitBarycentrics[1] == 0);
        //check(!bOriginallyZero[2] || SplitBarycentrics[2] == 0);
        Console.WriteLine(!bOriginallyZero[2] || SplitBarycentrics[2] == 0);

	    uint SplitVert = SplitBarycentrics[0] | (SplitBarycentrics[1] << 16);
	    uint SplitIndex = AddVert(SplitVert);

        //checkf(SplitIndex != i0 && SplitIndex != i1 && SplitIndex != i2, TEXT("Degenerate triangle generated") );
        Console.WriteLine("Degenerate Triangle Generated: " + (SplitIndex != i0 && SplitIndex != i1 && SplitIndex != i2));
	
	    //Replace v0
	    Indexes.ToList().Add(SplitIndex | (i1 << 10) | (i2 << 20));

	    //Replace v1
	    Indexes[TriIndex] = i0 | (SplitIndex << 10) | (i2 << 20);
	}

    //Longest edge bisection. Uses Diagsplit rules instead of exact bisection.
	private void RecursiveSplit(uint[] TessFactors)
	{
        // Start with patch triangle
        Verts.ToList().Add(BarycentricMax + 0);  // Avoids TArray:Add grabbing reference to constexpr and forcing ODR-use.
        Verts.ToList().Add(BarycentricMax << 16);
        Verts.ToList().Add(0);

        Indexes.ToList().Add(0 | (1 << 10) | (2 << 20));

        HashTable.Clear();
        HashTable.Add(Verts[0], 0);
        HashTable.Add(Verts[1], 1);
        HashTable.Add(Verts[2], 2);

        for(int TriIndex = FirstTri; TriIndex < Indexes.Length;)
        {
            float[] EdgeLength2 = new float[3];
            for(uint i = 0; i < 3; i++)
            {
                uint e0 = i;
                uint e1 = (uint)((1 << (int)e0) & 3);

                uint Triangle = Indexes[TriIndex];
                uint i0 = (Triangle >> (int)(e0 * 10)) & 1023;
                uint i1 = (Triangle >> (int)(e1 * 10)) & 1023;

                uint[] b0 = GetBarycentrics(Verts[FirstVert + i0]);
                uint[] b1 = GetBarycentrics(Verts[FirstVert + i1]);

                EdgeLength2[i] = LengthSquared(ArrayUtils.Subtract(b0, b1), TessFactors);
            }

            uint EdgeIndex = (uint)Math.Max((uint)Math.Max(EdgeLength2[0], EdgeLength2[1]), EdgeLength2[2]);
            //check(EdgeLength2[EdgeIndex] >= 0.0f);

            uint NumEdgeSplits = (uint)Math.Round(Math.Sqrt(EdgeLength2[EdgeIndex]));
            uint HalfSplit = NumEdgeSplits >> 1;

            if(NumEdgeSplits <= 1)
            {
                // Triangle is small enough
                TriIndex++;
                continue;
            }

            SplitEdge((uint)TriIndex, EdgeIndex, HalfSplit, NumEdgeSplits - HalfSplit, TessFactors);
        }
    }
	
	private void UniformTessellateAndSnap(uint[] TessFactors)
	{
        /*
	    ===========
		    v0
		    /\
	    e2 /  \ e0
	      /____\
	    v2  e1  v1
	    ===========
	    */

        HashTable.Clear();

        uint NumTris = TessFactors[0] * TessFactors[0];

        for(uint TriIndex = 0; TriIndex < NumTris; TriIndex++)
        {
            /*
                Starts from top point. Adds rows of verts and corresponding rows of tri strips.

                |\
            row |\|\
                |\|\|\
                column
            */

            // Find largest tessellation with NumTris <= TriIndex. These are the preceding tris before this row.
            uint TriRow = (uint)Math.Floor(Math.Sqrt(TriIndex));
            uint TriCol = TriIndex - TriRow * TriRow;
            /*
                Vert order:
                0    0__1
                |\   \  |
                | \   \ |  <= flip triangle
                |__\   \|
                2   1   2
            */
            uint FlipTri = TriCol & 1;
            uint VertCol = TriCol >> 1;

            uint[,] VertRowCol = new uint[3, 2]
            {
                { TriRow,		VertCol     },
			    { TriRow + 1,	VertCol + 1 },
			    { TriRow + 1,	VertCol     },
		    };
            VertRowCol[1, 0] -= FlipTri;
            VertRowCol[2, 1] += FlipTri;

            uint[] TriVerts = new uint[3] { 0, 0, 0 };
            for(int Corner = 0; Corner < 3; Corner++)
            {
                /*
                    b0
                    |\
                t2  | \  t0
                    |__\
                   b2   b1
                     t1
                */
                uint[] Barycentrics = new uint[3];
                Barycentrics[0] = TessFactors[0] - VertRowCol[Corner, 0];
                Barycentrics[1] = VertRowCol[Corner, 1];
                Barycentrics[2] = VertRowCol[Corner, 0] - VertRowCol[Corner, 1];

                Barycentrics[0] *= BarycentricMax;
                Barycentrics[1] *= BarycentricMax;
                Barycentrics[2] *= BarycentricMax;

                // Fixed point round
                Barycentrics[0] = (Barycentrics[0] + (BarycentricMax / 2) - 1) & ~(BarycentricMax - 1);
                Barycentrics[1] = (Barycentrics[1] + (BarycentricMax / 2) - 1) & ~(BarycentricMax - 1);
                Barycentrics[2] = (Barycentrics[2] + (BarycentricMax / 2) - 1) & ~(BarycentricMax - 1);

                Barycentrics[0] /= TessFactors[0];
                Barycentrics[1] /= TessFactors[0];
                Barycentrics[2] /= TessFactors[0];

                {
                    int e0 = (int)Math.Max(Math.Max(Barycentrics[0], Barycentrics[1]), Barycentrics[2]);
                    int e1 = (1 << e0) & 3;
                    int e2 = (1 << e1) & 3;

                    Barycentrics[e0] = BarycentricMax - Barycentrics[e1] - Barycentrics[e2];
                }

#if true
			for(int i = 0; i < 3; i++)
			{
				int e0 = i;
				int e1 = (1 << e0) & 3;
				int e2 = (1 << e1) & 3;

				if(Barycentrics[e0] == 0 || Barycentrics[e1] == 0 || Barycentrics[e2] == 0 )
					continue;

				uint Sum = Barycentrics[e0] + Barycentrics[e1];

#if false
				//Snap toward min barycentric means snapping mirrors.
				uint MinIndex = Barycentrics[ e0 ] <  Barycentrics[ e1 ] ? e0 : e1;
				uint MaxIndex = Barycentrics[ e0 ] >= Barycentrics[ e1 ] ? e0 : e1;

				//Fixed point round
				uint Snapped = ( Barycentrics[ MinIndex ] * TessFactors[i] + (BarycentricMax / 2) - 1 ) & ~( BarycentricMax - 1 );

				Barycentrics[MinIndex] = FMath::Min( Sum, Snapped / TessFactors[i]);
				Barycentrics[MaxIndex] = Sum - Barycentrics[MinIndex];

				if(Barycentrics[MinIndex] > Barycentrics[MaxIndex])
				{
					Barycentrics[e0] = Sum / 2;
					Barycentrics[e1] = Sum - Barycentrics[e0];
				}
#else
				//Fixed point round
				uint Snapped = (Barycentrics[e0] * TessFactors[i] + (BarycentricMax / 2) - 1) & ~(BarycentricMax - 1);

				Barycentrics[e0] = Math.Min(Sum, Snapped / TessFactors[i]);
				Barycentrics[e1] = Sum - Barycentrics[ e0 ];
#endif
			}
#endif

#if true
			//Snap verts to the edge if they are close.
			if(Barycentrics[0] != 0 && Barycentrics[1] != 0 && Barycentrics[2] != 0 )
			{
				// Find closest point on edge
				int b0 = (int)Math.Min(Math.Min(Barycentrics[0], Barycentrics[1]), Barycentrics[2]);
				int b1 = (1 << b0) & 3;
				int b2 = (1 << b1) & 3;

				//if( Barycentrics[ b1 ] < Barycentrics[ b2 ] )
				//	Swap( b1, b2 );

				uint Sum = Barycentrics[ b1 ] + Barycentrics[ b2 ];

				uint[] ClosestEdgePoint = new uint[3];
				ClosestEdgePoint[b0] = 0;
				ClosestEdgePoint[b1] = (Barycentrics[b1] * BarycentricMax) / Sum;
				ClosestEdgePoint[b2] = BarycentricMax - ClosestEdgePoint[b1];

				//Want edge point in its final position so we get the correct distance.
				SnapAtEdges(ClosestEdgePoint, TessFactors);

				float DistSqr = LengthSquared(ArrayUtils.Subtract(Barycentrics, ClosestEdgePoint), TessFactors);
				if(DistSqr < 0.25f)
				{
					Barycentrics = ClosestEdgePoint;
				}
			}
#endif

                SnapAtEdges(Barycentrics, TessFactors);

                TriVerts[Corner] = Barycentrics[0] | (Barycentrics[1] << 16);
            }

            //Degenerate
            if(TriVerts[0] == TriVerts[1] || TriVerts[1] == TriVerts[2] || TriVerts[2] == TriVerts[0])
                continue;

            uint[] VertIndexes = new uint[3];
            for(int Corner = 0; Corner < 3; Corner++)
                VertIndexes[Corner] = AddVert(TriVerts[Corner]);

            Indexes.ToList().Add(VertIndexes[0] | (VertIndexes[1] << 10) | (VertIndexes[2] << 20));
        }
    }
}