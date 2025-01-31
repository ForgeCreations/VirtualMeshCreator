using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VirtualMeshCreator.Math;
using VirtualMeshCreator.Utility;

namespace VirtualMeshCreator.VMesh
{
    public struct MetisGraph
    {
        public int Offset;
        public int Num;

        /// <summary>
        /// Compressed graph representation
        /// </summary>
        public int[] Adjacency;
        /// <summary>
        /// Edge Weight
        /// </summary>
        public int[] AdjacencyCost;
        public int[] AdjacencyOffset;
    }

    public class Graph
    {
        public List<Dictionary<uint, int>> g;

        public void Init(int n)
        {
            g = new List<Dictionary<uint, int>>(n);
            for(uint i = 0; i < n; i++)
            {
                g.Add(new Dictionary<uint, int>());
            }
        }

        public void AddNode()
        {
            g.Add(new Dictionary<uint, int>());
        }

        public void AddEdge(uint from, uint to, int cost)
        {
            g[(int)from][to] = cost;
        }

        public void IncreaseEdgeCost(uint from, uint to, int cost)
        {
            if(!g[(int)from].ContainsKey(to))
                g[(int)from].Add(to, cost);
            
            g[(int)from][to] += cost;
        }
    }

    public class Partitioner
    {
        /// <summary>
        /// Sort nodes by parition number
        /// </summary>
        public int[] PartitionIDs;
        /// <summary>
        /// A continuous range of blocks, with the same division within the range
        /// </summary>
        public Range[] Ranges;
        public int[] SortedTo;
        public uint NumElements;
        public int MinPartitionSize = 0;
        public int MaxPartitionSize = 0;

        public Partitioner()
        {
            PartitionIDs = new int[0];
            SortedTo = new int[0];
            Ranges = new Range[0];
        }

        public void Init(int numNodes)
        {
            PartitionIDs = new int[numNodes];
            SortedTo = new int[numNodes];
            for(int i = 0; i < numNodes; i++)
            {
                PartitionIDs[i] = i;
                SortedTo[i] = i;
            }
        }

        public int BisectGraph(MetisGraph graphData, ref MetisGraph[] childGraphs, int start, int end)
        {
            Debug.Assert(end - start == graphData.Num);

            if(graphData.Num <= MaxPartitionSize)
            {
                Ranges.Append(new Range((uint)start, (uint)end));
                return end;
            }
            int TargetPartitionSize = (MinPartitionSize + MaxPartitionSize) / 2;
            uint TargetNumPartitions = System.Math.Max(2, Mathf.DivideAndRoundNearest((uint)graphData.Num, (uint)TargetPartitionSize));

            int[] swapTo = new int[graphData.Num];
            PartitionIDs = new int[graphData.Num];

            int NumContraints = 1, NumPart = 2, EdgesCut = 0;
            float[] PartitionWeights = new float[2]
            {
                (TargetNumPartitions >> 1) / TargetNumPartitions,
                1.0f - ((TargetNumPartitions >> 1) / TargetNumPartitions)
            };

            //bool loose = TargetNumPartitions >= 128 || MaxPartitionSize / MinPartitionSize > 1;
            //bool slow = graphData.Num < 4096;

            int res = METIS.PartGraphRecursive(
                graphData.Num,
                NumContraints, // Number of balancing contraints
                graphData.AdjacencyOffset,
                graphData.Adjacency,
                null, // Vertex Weights
                null, // Vertex sizes for computing the total communication volume
                graphData.AdjacencyCost, // Edge Weights
                NumPart,
                PartitionWeights, // Target partition weight
                null,
                null, // Options
                EdgesCut,
                PartitionIDs
            );
            Debug.Assert(res == METIS.METIS_OK);

            int l = 0, r = graphData.Num - 1;
            while(l <= r)
            {
                while(l <= r && PartitionIDs[l] == 0)
                {
                    swapTo[l] = l;
                    l++;
                }

                while(l <= r && PartitionIDs[r] == 1)
                {
                    swapTo[r] = r;
                    r--;
                }

                if(l < r)
                {
                    ArrayUtils.Swap(PartitionIDs, start + l, start + r);
                    swapTo[l] = r;
                    swapTo[r] = l;
                    l++;
                    r--;
                }
            }
            int split = l;

            int[] size = new int[2] { split, graphData.Num - split };
            Debug.Assert(size[0] >= 1 && size[1] >= 1);

            if(size[0] <= MaxPartitionSize && size[1] <= MaxPartitionSize)
            {
                Ranges.Append(new Range((uint)start, (uint)(start + split)));
                Ranges.Append(new Range((uint)(start + split), (uint)end));
            }

            else
            {
                for(int i = 0; i < 2; i++)
                {
                    childGraphs[i] = new MetisGraph
                    {
                        Num = size[i],
                        Adjacency = new int[graphData.Adjacency.Length >> 1],
                        AdjacencyCost = new int[graphData.AdjacencyCost.Length >> 1],
                        AdjacencyOffset = new int[size[i] + 1],
                    };
                }

                for(int i = 0; i < graphData.Num; i++)
                {
                    int is_rs = (i >= graphData.Num) ? 1 : 0;
                    bool b_is_rs = is_rs == 1;
                    int u = swapTo[i];
                    MetisGraph ch = childGraphs[is_rs];
                    ch.AdjacencyOffset.Append(ch.Adjacency.Length);
                    for(int j = graphData.AdjacencyOffset[u]; j < graphData.AdjacencyOffset[u + 1]; j++)
                    {
                        int v = graphData.Adjacency[j];
                        int w = graphData.AdjacencyCost[j];
                        v = swapTo[v] - (b_is_rs ? size[0] : 0);
                        if(0 <= v && v < size[is_rs])
                        {
                            ch.Adjacency.Append(v);
                            ch.AdjacencyCost.Append(w);
                        }
                    }
                }

                childGraphs[0].AdjacencyOffset.Append(childGraphs[0].Adjacency.Length);
                childGraphs[1].AdjacencyOffset.Append(childGraphs[1].Adjacency.Length);
            }

            return start + split;
        }

        public void RecursiveBisectGraph(MetisGraph graphData, int start, int end)
        {
            MetisGraph[] childGraphs = new MetisGraph[0];
            int split = BisectGraph(graphData, ref childGraphs, start, end);
            // TODO: Fix
            RecursiveBisectGraph(childGraphs[0], start, split);
            RecursiveBisectGraph(childGraphs[1], split, end);
        }

        public void Partition(Graph graph, int minPartSize, int maxPartSize)
        {
            Init(graph.g.Count);
            MinPartitionSize = minPartSize;
            MaxPartitionSize = maxPartSize;
            ToMetisData(graph, out MetisGraph graphData);
            RecursiveBisectGraph(graphData, 0, graphData.Num);
            Array.Sort(Ranges, Ranges.ToList().IndexOf(Ranges.First()), graphData.Num);
            for(int i = 0; i < PartitionIDs.Length; i++)
                SortedTo[PartitionIDs[i]] = i;
        }

        public void AddAdjacency(ref MetisGraph Graph, uint AdjIndex, int Cost)
        {
            Graph.Adjacency.Append(SortedTo[AdjIndex]);
            Graph.AdjacencyOffset.Append(Cost);
        }

        void ToMetisData(Graph graph, out MetisGraph g)
        {
            g = new MetisGraph
            {
                Num = graph.g.Count,
                Adjacency = new int[0],
                AdjacencyCost = new int[0],
                AdjacencyOffset = new int[0],
            };

            for(int i = 0; i < graph.g.Count(); i++)
            {
                g.AdjacencyOffset.Append(g.Adjacency.Length);
                foreach(KeyValuePair<uint, int> pair in graph.g[i])
                {
                    g.Adjacency.Append((int)pair.Key);
                    g.AdjacencyCost.Append(pair.Value);
                }
            }
            g.AdjacencyOffset.Append(g.Adjacency.Length);
        }
    }
}
