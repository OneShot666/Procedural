using System.Collections.Generic;

namespace MST
{
    public class Graph
    {
        public int Vertices { get; set; }
        public List<Edge> Edges { get; set; }

        public Graph(
            int vertices)
        {
            Vertices = vertices;
            Edges = new List<Edge>();
        }

        public void AddEdge(
            int source,
            int destination,
            int weight)
        {
            Edges.Add(new Edge { Source = source, Destination = destination, Weight = weight });
        }
    }
}