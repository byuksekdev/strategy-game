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
    //
    // GC pressure:
    //   All internal working collections are static and cleared at the start of each
    //   FindPath() call so no heap allocations occur during the search itself.
    //   The returned List<Vector2Int> (caller-owned path) is the only allocation per call.
    //   Safe because Unity runs game logic on the main thread — FindPath() is not reentrant.
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

        // Static working collections reused across FindPath() calls.
        // Initial capacities cover typical grid searches without resizing.
        private static readonly List<HeapNode>                     _heap      = new List<HeapNode>(64);
        private static readonly HashSet<Vector2Int>                _inOpenSet = new HashSet<Vector2Int>();
        private static readonly Dictionary<Vector2Int, Vector2Int> _cameFrom  = new Dictionary<Vector2Int, Vector2Int>(128);
        private static readonly Dictionary<Vector2Int, int>        _gScore    = new Dictionary<Vector2Int, int>(128);

        // Scratch buffer for path reconstruction; result is copied into a correctly-sized list.
        private static readonly List<Vector2Int> _reconstructBuffer = new List<Vector2Int>(64);

        #region PUBLIC_METHODS

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, IGridProvider grid)
        {
            if (grid == null) return null;
            if (start == end) return new List<Vector2Int>();
            if (!grid.IsValidCoordinate(end)) return null;

            _heap.Clear();
            _inOpenSet.Clear();
            _cameFrom.Clear();
            _gScore.Clear();

            _inOpenSet.Add(start);
            _gScore[start] = 0;
            HeapPush(new HeapNode { FScore = Heuristic(start, end), Coord = start });

            while (_heap.Count > 0)
            {
                HeapNode   top     = HeapPop();
                Vector2Int current = top.Coord;

                // Lazy deletion: this entry was superseded by a cheaper one.
                if (!_inOpenSet.Contains(current)) continue;
                _inOpenSet.Remove(current);

                if (current == end)
                    return ReconstructPath(current);

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
                    int tentativeG = GetScore(current) + moveCost;

                    if (tentativeG >= GetScore(nc)) continue;

                    _cameFrom[nc] = current;
                    _gScore[nc]   = tentativeG;

                    _inOpenSet.Add(nc);
                    HeapPush(new HeapNode { FScore = tentativeG + Heuristic(nc, end), Coord = nc });
                }
            }

            return null; // No path found
        }

        #endregion

        #region PRIVATE_METHODS

        private static void HeapPush(HeapNode node)
        {
            _heap.Add(node);
            int i = _heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (_heap[parent].FScore <= _heap[i].FScore) break;
                (_heap[parent], _heap[i]) = (_heap[i], _heap[parent]);
                i = parent;
            }
        }

        private static HeapNode HeapPop()
        {
            HeapNode top  = _heap[0];
            int      last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int left     = 2 * i + 1;
                int right    = 2 * i + 2;
                int smallest = i;

                if (left  < _heap.Count && _heap[left].FScore  < _heap[smallest].FScore) smallest = left;
                if (right < _heap.Count && _heap[right].FScore < _heap[smallest].FScore) smallest = right;

                if (smallest == i) break;
                (_heap[i], _heap[smallest]) = (_heap[smallest], _heap[i]);
                i = smallest;
            }

            return top;
        }

        // Walks _cameFrom into _reconstructBuffer (avoids a separate allocation),
        // reverses in place, then copies to a correctly-sized caller-owned list.
        private static List<Vector2Int> ReconstructPath(Vector2Int current)
        {
            _reconstructBuffer.Clear();

            while (_cameFrom.ContainsKey(current))
            {
                _reconstructBuffer.Add(current);
                current = _cameFrom[current];
            }

            _reconstructBuffer.Reverse();

            var path = new List<Vector2Int>(_reconstructBuffer.Count);
            path.AddRange(_reconstructBuffer);
            return path;
        }

        // Returns the stored g-score or int.MaxValue/2 to avoid overflow when adding costs.
        private static int GetScore(Vector2Int key)
            => _gScore.TryGetValue(key, out int v) ? v : int.MaxValue / 2;

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
