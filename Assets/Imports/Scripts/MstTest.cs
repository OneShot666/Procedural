using System.Collections.Generic;
using UnityEngine;

namespace MST
{
    public class test : MonoBehaviour
    {
        void Start()
        {
            Graph graph = new Graph(4);
            graph.AddEdge(0, 1, 10);
            graph.AddEdge(0, 2, 6);
            graph.AddEdge(0, 3, 5);
            graph.AddEdge(1, 3, 15);
            graph.AddEdge(2, 3, 4);

            List<Edge> mst = KruskalMST.GetMinimumSpanningTree(graph);

            Debug.Log("Edges in the Minimum Spanning Tree:");
            foreach (var edge in mst)
            {
                Debug.Log($"Source: {edge.Source}, Destination: {edge.Destination}, Weight: {edge.Weight}");
            }
        }
    }
}
