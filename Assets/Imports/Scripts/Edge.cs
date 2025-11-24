using System;

namespace MST
{
    public class Edge : IComparable<Edge>
    {
        public int Source { get; set; }
        public int Destination { get; set; }
        public int Weight { get; set; }

        public int CompareTo(
            Edge other) =>
            Weight.CompareTo(other.Weight);
    }
}