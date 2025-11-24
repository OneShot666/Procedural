using System.Collections.Generic;

namespace MST
{
    public class KruskalMST
    {
        public static List<Edge> GetMinimumSpanningTree(
            Graph graph)
        {
            List<Edge> result = new List<Edge>();
            int vertices = graph.Vertices;
            DisjointSet disjointSet = new DisjointSet(vertices);

            graph.Edges.Sort();

            foreach (Edge edge in graph.Edges)
            {
                int rootSource = disjointSet.Find(edge.Source);
                int rootDestination = disjointSet.Find(edge.Destination);

                if (rootSource != rootDestination)
                {
                    result.Add(edge);
                    disjointSet.Union(
                        rootSource,
                        rootDestination
                    );
                }
            }

            return result;
        }
    }
}