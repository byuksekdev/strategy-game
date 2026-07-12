using System.Collections.Generic;
using UnityEngine;
using StrategyGame.Grid;
using StrategyGame.Units;

namespace StrategyGame.Pathfinding
{
    // Classic A* pathfinder operating on an IGridProvider.
    // Stateless static class — create no instances; call FindPath() directly.
    //
    // Rules:
    //   • 8-directional movement (diagonals allowed, cost ≈ √2 × straight).
    //   • Diagonal moves are blocked when either adjacent cardinal cell is non-walkable
    //     (no corner-cutting through building edges).
    //   • Non-walkable cells (buildings) are blocked, EXCEPT the target cell itself
    //     so that attacking a building can still produce a valid path to its edge.
    //   • Unit-occupied cells are blocked, EXCEPT the target cell itself
    //     (allows attack-move commands toward an occupied cell).
    //   • Returns null when no path exists; empty list when start == end.
    //   • Returned path: [first step … target], does NOT include start.
    //
    // Costs (scaled integers to avoid floating-point drift):
    //   Straight move  → 10
    //   Diagonal move  → 14  (≈ 10 × √2)
    //
    // Open-set structure: binary min-heap with lazy deletion.
    //   When a node's g-score is improved, a new entry is pushed to the heap and
    //   the old entry is left in place. Stale entries are skipped when popped
    //   (they will not be in inOpenSet). This gives O(log n) push/pop instead of
    //   the previous O(n) linear scan, reducing worst-case cost from O(n²) to O(n log n).
    public static class AStarPathfinder
    {
        //-------Public Variables-------//

        //------Private Variables-------//
        private const int StraightCost = 10;
        private const int DiagonalCost = 14;

        private struct HeapNode
        {
            public int        FScore;
            public Vector2Int Coord;
        }

        #region PUBLIC_METHODS

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, IGridProvider grid)
        {
            if (grid == null) return null;
            if (start == end) return new List<Vector2Int>();
            if (!grid.IsValidCoordinate(end)) return null;

            var heap      = new List<HeapNode>();
            var inOpenSet = new HashSet<Vector2Int> { start };
            var cameFrom  = new Dictionary<Vector2Int, Vector2Int>();
            var gScore    = new Dictionary<Vector2Int, int> { [start] = 0 };

            HeapPush(heap, new HeapNode { FScore = Heuristic(start, end), Coord = start });

            while (heap.Count > 0)
            {
                HeapNode top     = HeapPop(heap);
                Vector2Int current = top.Coord;

                // Lazy deletion: this entry was superseded by a cheaper one.
                if (!inOpenSet.Contains(current)) continue;
                inOpenSet.Remove(current);

                if (current == end)
                    return ReconstructPath(cameFrom, current);

                GridCell currentCell = grid.GetCell(current);
                if (currentCell == null) continue;

                foreach (GridCell neighbour in grid.GetNeighbors(currentCell, includeDiagonals: true))
                {
                    Vector2Int nc = neighbour.Coordinate;

                    // Block non-walkable cells (buildings) unless they are the target.
                    if (!neighbour.IsWalkable && nc != end) continue;

                    // Block unit-occupied cells unless they are the target.
                    if (UnitRegistry.IsCellOccupied(nc) && nc != end) continue;

                    bool isDiagonal = (nc.x != current.x) && (nc.y != current.y);

                    // Prevent corner cutting: both cardinal cells between current and
                    // the diagonal neighbour must be walkable (no squeezing through corners).
                    if (isDiagonal)
                    {
                        GridCell cardinalA = grid.GetCell(current.x, nc.y);
                        GridCell cardinalB = grid.GetCell(nc.x, current.y);
                        if (cardinalA == null || !cardinalA.IsWalkable ||
                            cardinalB == null || !cardinalB.IsWalkable)
                            continue;
                    }

                    int moveCost   = isDiagonal ? DiagonalCost : StraightCost;
                    int tentativeG = GetScore(gScore, current) + moveCost;

                    if (tentativeG >= GetScore(gScore, nc)) continue;

                    cameFrom[nc] = current;
                    gScore[nc]   = tentativeG;

                    int newF = tentativeG + Heuristic(nc, end);
                    inOpenSet.Add(nc);
                    HeapPush(heap, new HeapNode { FScore = newF, Coord = nc });
                }
            }

            return null; // No path found
        }

        #endregion

        #region PRIVATE_METHODS

        private static void HeapPush(List<HeapNode> heap, HeapNode node)
        {
            heap.Add(node);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (heap[parent].FScore <= heap[i].FScore) break;
                (heap[parent], heap[i]) = (heap[i], heap[parent]);
                i = parent;
            }
        }

        private static HeapNode HeapPop(List<HeapNode> heap)
        {
            HeapNode top  = heap[0];
            int      last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int left     = 2 * i + 1;
                int right    = 2 * i + 2;
                int smallest = i;

                if (left  < heap.Count && heap[left].FScore  < heap[smallest].FScore) smallest = left;
                if (right < heap.Count && heap[right].FScore < heap[smallest].FScore) smallest = right;

                if (smallest == i) break;
                (heap[i], heap[smallest]) = (heap[smallest], heap[i]);
                i = smallest;
            }

            return top;
        }

        private static List<Vector2Int> ReconstructPath(
            Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int>();

            while (cameFrom.ContainsKey(current))
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Reverse();
            return path;
        }

        // Returns the stored score or int.MaxValue/2 to avoid overflow when adding costs.
        private static int GetScore(Dictionary<Vector2Int, int> dict, Vector2Int key)
            => dict.TryGetValue(key, out int v) ? v : int.MaxValue / 2;

        // Octile distance (scaled by 10): admissible heuristic for 8-directional grids.
        // Formula: StraightCost × max(dx,dy) + (DiagonalCost − StraightCost) × min(dx,dy)
        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return StraightCost * Mathf.Max(dx, dy) + (DiagonalCost - StraightCost) * Mathf.Min(dx, dy);
        }

        #endregion
    }
}
