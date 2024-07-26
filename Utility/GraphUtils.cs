using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualMeshCreator.VMesh;

namespace VirtualMeshCreator.Utility
{
    public static class GraphUtils
    {
        public static MetisGraph ToMetisData(this MetisGraph g, ref Graph graph)
        {
            //MetisGraph g = new MetisGraph();
            g.Num = graph.g.Count;

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
            return g;
        }
    }
}
