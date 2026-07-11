using UnityEngine;

namespace StrategyGame.Pathfinding
{
    // A single node in the A* open/closed set.
    // Stored by value (struct) to avoid heap allocations during pathfinding.
    public struct PathNode
    {
        //-------Public Variables-------//
        public Vector2Int Coordinate;
        public int GCost;           // Accumulated cost from start to this node.
        public int HCost;           // Heuristic estimate from this node to goal (Octile, scaled ×10).
        public int FCost => GCost + HCost;
        public Vector2Int Parent;   // Coordinate of the node this was reached from.
        public bool HasParent;      // False only for the start node.
    }
}
