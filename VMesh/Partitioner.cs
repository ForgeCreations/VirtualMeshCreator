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

    public struct Graph
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
        public uint[] nodeIDs;
        /// <summary>
        /// A continuous range of blocks, with the same division within the range
        /// </summary>
        public Pair<int, int>[] ranges;
        public uint[] sortTo;
        public int minPartSize;
        public int maxPartSize;

        public Partitioner()
        {
            nodeIDs = new uint[0];
            sortTo = new uint[0];
            ranges = new Pair<int, int>[0];
        }

        public void Init(int numNodes)
        {
            nodeIDs = new uint[numNodes];
            sortTo = new uint[numNodes];
            for(uint i = 0; i < numNodes; i++)
            {
                nodeIDs[i] = i;
                sortTo[i] = i;
            }
        }

        public int BisectGraph(MetisGraph graphData, ref MetisGraph[] childGraphs, int start, int end)
        {
            Debug.Assert(end - start == graphData.Num);

            if(graphData.Num <= maxPartSize)
            {
                ranges.Append(new Pair<int, int>(start, end));
                return end;
            }
            int expPartSize = (minPartSize + maxPartSize) / 2;
            int expNumParts = System.Math.Max(2, (graphData.Num + expPartSize - 1) / expPartSize);

            int[] swapTo = new int[graphData.Num];
            int[] part = new int[graphData.Num];

            int nw = 1, npart = 2, ncut = 0;
            float[] part_weight =
            {
                (expNumParts >> 1) / expNumParts,
                1.0f - ((expNumParts >> 1) / expNumParts)
            };

            int res = METIS.METIS_PartGraphRecursive(
                ref graphData.Num,
                ref nw,
                graphData.AdjacencyOffset,
                graphData.Adjacency,
                null, // Vertex Weights
                null, // Vertex Size
                graphData.AdjacencyCost,
                ref npart,
                part_weight, // Partition Weight
                null,
                null, // Options
                ref ncut,
                part
            );
            Debug.Assert(res == METIS.METIS_OK);

            int l = 0, r = graphData.Num - 1;
            while(l <= r)
            {
                while(l <= r && part[l] == 0)
                {
                    swapTo[l] = l;
                    l++;
                }

                while(l <= r && part[r] == 1)
                {
                    swapTo[r] = r;
                    r--;
                }

                if(l < r)
                {
                    ArrayUtils.Swap(nodeIDs, start + l, start + r);
                    swapTo[l] = r;
                    swapTo[r] = l;
                    l++;
                    r--;
                }
            }
            int split = l;

            int[] size = new int[2] { split, graphData.Num - split };
            Debug.Assert(size[0] >= 1 && size[1] >= 1);

            if(size[0] <= maxPartSize && size[1] <= maxPartSize)
            {
                ranges.Append(new Pair<int, int>(start, start + split));
                ranges.Append(new Pair<int, int>(start + split, end));
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
                    bool b_is_rs = is_rs == 1 ? true : false;
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
            this.minPartSize = minPartSize;
            this.maxPartSize = maxPartSize;
            ToMetisData(graph, out MetisGraph graphData);
            RecursiveBisectGraph(graphData, 0, graphData.Num);
            Array.Sort(ranges, ranges.ToList().IndexOf(ranges.First()), graphData.Num);
            for(uint i = 0; i < nodeIDs.Length; i++)
                sortTo[nodeIDs[i]] = i;
        }

        public void AddAdjacency(ref MetisGraph Graph, uint AdjIndex, int Cost)
        {
            Graph.Adjacency.Append((int)sortTo[AdjIndex]);
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

            for(int i = 0; i < graph.g.Count; i++)
            {
                g.AdjacencyOffset.Append(g.Adjacency.Length);
                foreach(KeyValuePair<uint, int> kvp in graph.g[i])
                {
                    g.Adjacency.Append((int)kvp.Key);
                    g.AdjacencyCost.Append(kvp.Value);
                }
            }
            g.AdjacencyOffset.Append(g.Adjacency.Length);
        }
    }
}
