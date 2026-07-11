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
    public static class AStarPathfinder
    {
        //-------Public Variables-------//

        //------Private Variables-------//
        private const int StraightCost = 10;
        private const int DiagonalCost = 14;

        #region PUBLIC_METHODS

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, IGridProvider grid)
        {
            if (grid == null) return null;
            if (start == end) return new List<Vector2Int>();

            if (!grid.IsValidCoordinate(end)) return null;

            var openSet   = new HashSet<Vector2Int> { start };
            var cameFrom  = new Dictionary<Vector2Int, Vector2Int>();
            var gScore    = new Dictionary<Vector2Int, int> { [start] = 0 };
            var fScore    = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, end) };

            while (openSet.Count > 0)
            {
                Vector2Int current = PickLowestF(openSet, fScore);

                if (current == end)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

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

                    int moveCost     = isDiagonal ? DiagonalCost : StraightCost;
                    int tentativeG   = GetScore(gScore, current) + moveCost;

                    if (tentativeG >= GetScore(gScore, nc)) continue;

                    cameFrom[nc] = current;
                    gScore[nc]   = tentativeG;
                    fScore[nc]   = tentativeG + Heuristic(nc, end);
                    openSet.Add(nc);
                }
            }

            return null; // No path found
        }

        #endregion

        #region PRIVATE_METHODS

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

        // Selects the node with the lowest f-score from the open set.
        private static Vector2Int PickLowestF(
            HashSet<Vector2Int> openSet, Dictionary<Vector2Int, int> fScore)
        {
            Vector2Int best  = default;
            int        bestF = int.MaxValue;

            foreach (Vector2Int coord in openSet)
            {
                int f = GetScore(fScore, coord);
                if (f < bestF) { bestF = f; best = coord; }
            }

            return best;
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
