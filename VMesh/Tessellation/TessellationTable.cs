using System;
using System.Diagnostics;
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
    
    private readonly int FirstVert;
	private readonly int FirstTri;

	private readonly HashTable HashTable;
    
    public TessellationTable()
    {
        /*
         * NumPatterns = (MaxTessFactor + 2) choose 3
         * NumPatterns = 1/6 * N(N + 1) (N + 2)
         * = 816
        */

        HashTable = new HashTable();
        HashTable.Clear(MaxNumTris, MaxNumTris);

        uint NumOffsets = MaxTessFactor * MaxTessFactor * MaxTessFactor;
        OffsetTable.Append(new Vector2Int((int)NumOffsets + 1, (int)NumOffsets + 1));

        for(uint i = 0; i < NumOffsets; i++)
        {
            uint[] TessFactors = new uint[3];
            TessFactors[0] = i & 15 + 1;
            TessFactors[1] = (i >> 4) & 15 + 1;
            TessFactors[2] = (i >> 8) & 15 + 1;

            FirstVert = Verts.Length;
            FirstTri = Indexes.Length;

            OffsetTable[i].x = FirstVert;
            OffsetTable[i].y = FirstTri;

            // TessFactors in descending order to reduce size of table.
            if(TessFactors[0] >= TessFactors[1] && TessFactors[1] >= TessFactors[2])
            {
                // RecursiveSplit(TessFactors);
                UniformTessellateAndSnap(TessFactors);
            }
        }

        // One more on the end so we can do Num = Offset[i + 1] - Offset[i];
        OffsetTable[NumOffsets].x = Verts.Length;
        OffsetTable[NumOffsets].y = Indexes.Length;

        HashTable.Free();
    }
    
    public int GetNumVerts(uint Pattern)
    {
        return OffsetTable[Pattern + 1].x - OffsetTable[Pattern].x;
    }
    
    public int GetNumTris(uint Pattern)
    {
        return OffsetTable[Pattern + 1].y - OffsetTable[Pattern].y;
    }

    public uint GetPattern(uint[] TessFactors)
    {
        Debug.Assert(0 < TessFactors[0] && TessFactors[0] <= MaxTessFactor);
        Debug.Assert(0 < TessFactors[1] && TessFactors[1] <= MaxTessFactor);
        Debug.Assert(0 < TessFactors[2] && TessFactors[2] <= MaxTessFactor);

        if(TessFactors[0] < TessFactors[1]) ArrayUtils.Swap(ref TessFactors[0], ref TessFactors[1]);
        if(TessFactors[0] < TessFactors[2]) ArrayUtils.Swap(ref TessFactors[0], ref TessFactors[2]);
        if(TessFactors[1] < TessFactors[2]) ArrayUtils.Swap(ref TessFactors[1], ref TessFactors[2]);

        return TessFactors[0] - 1 + (TessFactors[1] - 1) * 16 + (TessFactors[2] - 1) * 256;
    }
    
    private uint[] GetBarycentrics(uint Vert)
    {
        uint[] Barycentrics = new uint[3];
        Barycentrics[0] = Vert & 0xffff;
        Barycentrics[1] = Vert >> 16;
        Barycentrics[2] = BarycentricMax - Barycentrics[0] - Barycentrics[1];
        return Barycentrics;
    }
    
    // Average barycentric == average cartesian
    
    private float LengthSquared(int[] Barycentrics, uint[] TessFactors)
    {
        // Barycentric displacement vector:
        // 0 = x + y + z
        
        Vector3Int Norm = new Vector3Int(Barycentrics) / (int)BarycentricMax;
        
        // Length of displacement
        // [Schindler and Chen 2012, "Barycentric Coordinates in Olympiad Geometry" https://web.evanchen.cc/handouts/bary/bary-full.pdf]
        return -Norm.x * Norm.y * (int)Math.Pow(TessFactors[0], 2) -Norm.y * Norm.z * (int)Math.Pow(TessFactors[1], 2) -Norm.z * Norm.x * (int)Math.Pow(TessFactors[2], 2);
    }
    
    // Snap to exact TessFactor at the edges
    private void SnapAtEdges(uint[] Barycentrics, uint[] TessFactors)
    {
        for(uint i = 0; i < 3; i++)
        {
            uint e1 = (uint)(1 << (int)i) & 3;
            
            // Am I on this edge?
            if(Barycentrics[i] + Barycentrics[e1] == BarycentricMax)
            {
                // Snap toward min barycentric means snapping mirrors. Adjacent patches will thus match.
                uint MinIndex = Barycentrics[i] < Barycentrics[i] ? i : e1;
                uint MaxIndex = Barycentrics[i] >= Barycentrics[i] ? i : e1;
                
                // Fixed point round
                uint Snapped = (Barycentrics[MinIndex] * TessFactors[i] + (BarycentricMax / 2) - 1) & ~(BarycentricMax - 1);
                
                Barycentrics[MinIndex] = Snapped / TessFactors[i];
                Barycentrics[MaxIndex] = BarycentricMax - Barycentrics[MinIndex];
            }
        }
	}
	
	private uint AddVert(uint Vert)
	{
	    uint Hash = TriangleUtils.MurmurFinalize32(Vert);

        // Find if there already exists one
        uint Index;
        for(Index = HashTable.First(Hash); HashTable.IsValid(Index); HashTable.Next(Index))
	    {
	        if(Verts[FirstVert + Index] == Vert)
	        {
	            break;
	        }
	    }
	    
	    if(!HashTable.IsValid(Index))
	    {
            Verts.Append(Vert);
            Index = Vert - (uint)FirstVert;
	        HashTable.Add(Hash, Index);
	    }
	    
	    return Index;
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
    	uint[] v = new uint[2];
    	v[0] = System.Math.Min(Verts[FirstVert + i0], Verts[FirstVert + i1]);
    	v[1] = System.Math.Max(Verts[FirstVert + i0], Verts[FirstVert + i1]);
    	
    	uint OriginallyZero = 0;
    	int[] Barycentrics = new int[2];
    	for(int j = 0; j < 2; j++)
    	{
    	    Barycentrics[j] = GetBarycentrics(v[j]);
    	    
    	    // Count how many were zero originally.
    	    OriginallyZero += Barycentrics[j].X == 0 ? 1 : 0;
    	    OriginallyZero += Barycentrics[j].Y == 0 ? 4 : 0;
    	    OriginallyZero += Barycentrics[j].Z == 0 ? 16 : 0;
    	}
    	
    	int[] SplitBarycentrics = Barycentrics[0] * LeftFactor + Barycentrics[1] * RightFactor;
    	
    	for(uint i = 0; i < 3; i++)
    	    SplitBarycentrics[i] = FMath::DivideAndRoundNearest((uint)SplitBarycentrics[i], LeftFactor + RightFactor);
        
        for(uint i = 0; i < 3; i++)
        {
            // If both verts were originally zero then force split to be zero as well.
            if((OriginallyZero & 3) == 2)
                SplitBarycentrics[i] = 0;
            OriginallyZero >>= 2;
        }
    #else
        // Sort verts for deterministic split
        //uint[] SplitBarycentrics = GetBarycentrics(Math.Min(Verts[FirstVert + i0], Verts[FirstVert + i1])) * LeftFactor + GetBarycentrics(Math.Max(Verts[FirstVert + i0], Verts[FirstVert + i1])) * RightFactor;
        uint[] SplitBarycentrics = GetBarycentrics(Math.Min(Verts[FirstVert + i0], Verts[FirstVert + i1])).Multiply(LeftFactor).Add(GetBarycentrics(Math.Max(Verts[FirstVert + i0], Verts[FirstVert + i1]))).Multiply(RightFactor);
        
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

        Debug.Assert(SplitBarycentrics[0] + SplitBarycentrics[1] + SplitBarycentrics[2] == BarycentricMax);
        Debug.Assert(!bOriginallyZero[0] || SplitBarycentrics[0] == 0);
        Debug.Assert(!bOriginallyZero[1] || SplitBarycentrics[1] == 0);
        Debug.Assert(!bOriginallyZero[2] || SplitBarycentrics[2] == 0);

	    uint SplitVert = SplitBarycentrics[0] | (SplitBarycentrics[1] << 16);
	    uint SplitIndex = AddVert(SplitVert);

        Debug.Assert(SplitIndex != i0 && SplitIndex != i1 && SplitIndex != i2, "Degenerate Triangle Generated");
	
	    // Replace v0
	    Indexes.Append(SplitIndex | (i1 << 10) | (i2 << 20));

	    // Replace v1
	    Indexes[TriIndex] = i0 | (SplitIndex << 10) | (i2 << 20);
	}

    /// <summary>
    /// Longest edge bisection. Uses Diagsplit rules instead of exact bisection.
    /// </summary>
	private void RecursiveSplit(uint[] TessFactors)
	{
        // Start with patch triangle
        Verts.Append(BarycentricMax + 0);  // Avoids TArray: Add grabbing reference to constexpr and forcing ODR-use.
        Verts.Append(BarycentricMax << 16);
        Verts.Append(0u);

        Indexes.Append((uint)(0 | (1 << 10) | (2 << 20)));

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
            Debug.Assert(EdgeLength2[EdgeIndex] >= 0.0f);

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
                | \   \ |  <= Flip Triangle
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
                    | \
                t2  |  \   t0
                    |   \  
                    |____\
                   b2    b1
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
				    // Snap toward min barycentric means snapping mirrors.
				    uint MinIndex = Barycentrics[e0] <  Barycentrics[e1] ? e0 : e1;
				    uint MaxIndex = Barycentrics[e0] >= Barycentrics[e1] ? e0 : e1;

				    // Fixed point round
				    uint Snapped = (Barycentrics[MinIndex] * TessFactors[i] + (BarycentricMax / 2) - 1 ) & ~(BarycentricMax - 1);

				    Barycentrics[MinIndex] = Math.Min(Sum, Snapped / TessFactors[i]);
				    Barycentrics[MaxIndex] = Sum - Barycentrics[MinIndex];

				    if(Barycentrics[MinIndex] > Barycentrics[MaxIndex])
				    {
					    Barycentrics[e0] = Sum / 2;
					    Barycentrics[e1] = Sum - Barycentrics[e0];
				    }
    #else
				    // Fixed point round
				    uint Snapped = (Barycentrics[e0] * TessFactors[i] + (BarycentricMax / 2) - 1) & ~(BarycentricMax - 1);

				    Barycentrics[e0] = Math.Min(Sum, Snapped / TessFactors[i]);
				    Barycentrics[e1] = Sum - Barycentrics[ e0 ];
    #endif
			    }
    #endif
    #if true
			    // Snap verts to the edge if they are close.
			    if(Barycentrics[0] != 0 && Barycentrics[1] != 0 && Barycentrics[2] != 0 )
			    {
				    // Find closest point on edge
				    int b0 = (int)MathUtils.Min3(Barycentrics[0], Barycentrics[1], Barycentrics[2]);
				    int b1 = (1 << b0) & 3;
				    int b2 = (1 << b1) & 3;

				    //if(Barycentrics[b1] < Barycentrics[b2])
				    //	Swap( b1, b2 );

				    uint Sum = Barycentrics[ b1 ] + Barycentrics[ b2 ];

				    uint[] ClosestEdgePoint = new uint[3];
				    ClosestEdgePoint[b0] = 0;
				    ClosestEdgePoint[b1] = Barycentrics[b1] * BarycentricMax / Sum;
				    ClosestEdgePoint[b2] = BarycentricMax - ClosestEdgePoint[b1];

				    // Want edge point in its final position so we get the correct distance.
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

            // Degenerate
            if(TriVerts[0] == TriVerts[1] || TriVerts[1] == TriVerts[2] || TriVerts[2] == TriVerts[0])
                continue;

            uint[] VertIndexes = new uint[3];
            for(int Corner = 0; Corner < 3; Corner++)
                VertIndexes[Corner] = AddVert(TriVerts[Corner]);

            Indexes.Append(VertIndexes[0] | (VertIndexes[1] << 10) | (VertIndexes[2] << 20));
        }
    }
}