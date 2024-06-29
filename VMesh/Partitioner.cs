using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualMeshCreator.Utility;

namespace VirtualMeshCreator.VMesh
{
    public struct MetisGraph
    {
        public int nvtxs;
        public int[] xadj;
        //Compressed graph representation
        public int[] adjncy;
        //Edge Weight
        public int[] adjwgt;
    }

    public struct Graph
    {
        public Dictionary<int, int>[] g;

        public Graph(int n)
        {
            g = new Dictionary<int, int>[0];
            Array.Resize(ref g, n);
        }

        public void AddNode()
        {
            g.ToList().Add(new Dictionary<int, int>());
        }

        public void AddEdge(int from, int to, int cost)
        {
            g[from][to] = cost;
        }

        public void IncreaseEdgeCost(int from, int to, int cost)
        {
            g[from][to] += cost;
        }

        public MetisGraph ToMetisData()
        {
            MetisGraph graph = new MetisGraph
            {
                nvtxs = g.Length
            };
            foreach(Dictionary<int, int> mp in g)
            {
                graph.xadj.ToList().Add(graph.adjncy.Length);
                foreach(KeyValuePair<int, int> vp in mp)
                {
                    graph.adjncy.ToList().Add(vp.Key);
                    graph.adjwgt.ToList().Add(vp.Value);
                }
            }
            graph.xadj.ToList().Add(graph.adjncy.Length);
            return graph;
        }
    }

    public class Partitioner
    {
        //Sort nodes by parition number
        public int[] nodeIDs;
        //A continuous range of blocks, with the same division within the range
        public Pair<int, int>[] ranges;
        public int[] sortTo;
        public int minPartSize;
        public int maxPartSize;

        public Partitioner(int numNodes)
        {
            nodeIDs = new int[numNodes];
            sortTo = new int[numNodes];
            int i;
            for(i = 0; i < numNodes; i++)
            {
                nodeIDs[i] = i;
                sortTo[i] = i;
                i++;
            }
        }

        public int BisectGraph(MetisGraph graphData, MetisGraph[] childGraphs, int start, int end)
        {
            if(graphData.nvtxs <= maxPartSize)
            {
                ranges.ToList().Add(new Pair<int, int>(start, end));
                return end;
            }
            int expPartSize = (minPartSize + maxPartSize) / 2;
            int expNumParts = System.Math.Max(2, (graphData.nvtxs + expPartSize - 1) / expPartSize);

            int[] swapTo = new int[graphData.nvtxs];
            //int[] part = new int[graphData.nvtxs];

            int nw = 1, npart = 2, ncut = 0;
            float[] part_weight = {
                (expNumParts >> 1) / expNumParts,
                1.0f - ((expNumParts >> 1) / expNumParts)
            };

            int res = METIS.PartGraphRecursive(
                graphData.nvtxs,
                nw,
                graphData.xadj,
                graphData.adjncy,
                null, //Vertex Weights
                null, //Vertex Size
                graphData.adjwgt,
                npart,
                part_weight, //Partition Weight
                null,
                null, //Options
                ncut,
                out int[] part
            );
            Console.WriteLine("METIS OK: " + (res == METIS.METIS_OK));

            int l = 0, r = graphData.nvtxs - 1;
            while(l <= r)
            {
                while(l <= r && part[l] == 0) swapTo[l] = l; l++;
                while(l <= r && part[r] == 1) swapTo[r] = r; r--;
                if(l < r)
                {
                    ArrayUtils.Swap(nodeIDs, start + l, start + r);
                    swapTo[l] = r; swapTo[r] = l;
                    l++; r--;
                }
            }
            int split = l;

            int[] size = { split, graphData.nvtxs - split };
            Console.WriteLine(size[0] >= 1 && size[1] >= 1);

            if(size[0] <= maxPartSize && size[1] <= maxPartSize)
            {
                ranges.ToList().Add(new Pair<int, int>(start, start + split));
                ranges.ToList().Add(new Pair<int, int>(start + split, end));
            }

            else
            {
                for(int i = 0; i < 2; i++)
                {
                    childGraphs[i] = new MetisGraph();
                    Array.Resize(ref childGraphs[i].adjncy, graphData.adjncy.Length >> 1);
                    Array.Resize(ref childGraphs[i].adjwgt, graphData.adjwgt.Length >> 1);
                    Array.Resize(ref childGraphs[i].xadj, size[i] + 1);
                    childGraphs[i].nvtxs = size[i];
                }

                for(int i = 0; i < graphData.nvtxs; i++)
                {
                    int is_rs = (i >= graphData.nvtxs) ? 1 : 0;
                    bool b_is_rs = is_rs == 1 ? true : false;
                    int u = swapTo[i];
                    MetisGraph ch = childGraphs[is_rs];
                    ch.xadj.ToList().Add(ch.adjncy.Length);
                    for(int j = graphData.xadj[u]; j < graphData.xadj[u + 1]; j++)
                    {
                        int v = graphData.adjncy[j];
                        int w = graphData.adjwgt[j];
                        v = swapTo[v] - (b_is_rs ? size[0] : 0);
                        if(0 <= v && v < size[is_rs])
                        {
                            ch.adjncy.ToList().Add(v);
                            ch.adjwgt.ToList().Add(w);
                        }
                    }
                }

                childGraphs[0].xadj.ToList().Add(childGraphs[0].adjncy.Length);
                childGraphs[1].xadj.ToList().Add(childGraphs[1].adjncy.Length);
            }

            return start + split;
        }

        public void RecursiveBisectGraph(MetisGraph graphData, int start, int end)
        {
            MetisGraph[] childGraphs = new MetisGraph[0];
            int split = BisectGraph(graphData, childGraphs, start, end);
            RecursiveBisectGraph(childGraphs[0], start, split);
            RecursiveBisectGraph(childGraphs[1], split, end);
        }

        public void Partition(ref Graph graph, int minPartSize, int maxPartSize)
        {
            this.minPartSize = minPartSize;
            this.maxPartSize = maxPartSize;
            MetisGraph graphData = graph.ToMetisData();
            RecursiveBisectGraph(graphData, 0, graphData.nvtxs);
            Array.Sort(ranges, ranges.ToList().IndexOf(ranges.First()), ranges.Length);
            for(int i = 0; i < nodeIDs.Length; i++)
                sortTo[nodeIDs[i]] = i;
        }
    }
}
