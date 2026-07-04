using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame.Grid
{
    // Extension methods for IGridProvider operating on rectangular grid areas.
    // SRP: GridManager owns state; this utility provides stateless, composable operations.
    public static class GridAreaUtility
    {
        // Lazy iterator over every valid cell in the area. Out-of-bounds coords are skipped.
        public static IEnumerable<GridCell> GetAreaCells(
            this IGridProvider grid, Vector2Int origin, Vector2Int size)
        {
            for (int x = origin.x; x < origin.x + size.x; x++)
            for (int y = origin.y; y < origin.y + size.y; y++)
            {
                GridCell cell = grid.GetCell(x, y);
                if (cell != null) yield return cell;
            }
        }

        // Returns all valid cells forming the border ring around the area (margin cells thick).
        // Used for spawn point detection and pathfinding invalidation around buildings.
        public static IEnumerable<GridCell> GetCellsAroundArea(
            this IGridProvider grid, Vector2Int origin, Vector2Int size, int margin = 1)
        {
            int xMin = origin.x - margin;
            int yMin = origin.y - margin;
            int xMax = origin.x + size.x + margin - 1;
            int yMax = origin.y + size.y + margin - 1;

            for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
            {
                bool insideInterior = x >= origin.x && x < origin.x + size.x
                                   && y >= origin.y && y < origin.y + size.y;
                if (insideInterior) continue;

                GridCell cell = grid.GetCell(x, y);
                if (cell != null) yield return cell;
            }
        }

        // Returns the first walkable cell adjacent to the area, or null if none exists.
        // Used to find the spawn point next to a Barracks.
        public static GridCell FindFirstFreeAdjacentCell(
            this IGridProvider grid, Vector2Int origin, Vector2Int size)
        {
            foreach (GridCell cell in grid.GetCellsAroundArea(origin, size, margin: 1))
                if (cell.IsWalkable) return cell;

            return null;
        }

        // Returns the world-space centre of the rectangular area.
        public static Vector3 GetAreaWorldCenter(
            this IGridProvider grid, Vector2Int origin, Vector2Int size)
        {
            // GridToWorld(origin) = centre of the bottom-left cell.
            // Shift by (size - 1) * 0.5 to reach the centre of the full area.
            Vector3 originCenter = grid.GridToWorld(origin);
            return originCenter + new Vector3(
                (size.x - 1) * 0.5f * grid.CellSize,
                (size.y - 1) * 0.5f * grid.CellSize,
                0f);
        }

        // Returns the world-space position of the bottom-left corner (not the cell centre).
        public static Vector3 GetAreaWorldBottomLeft(
            this IGridProvider grid, Vector2Int origin)
        {
            return grid.GridToWorld(origin) - new Vector3(
                grid.CellSize * 0.5f,
                grid.CellSize * 0.5f,
                0f);
        }

        // Returns the grid-space BoundsInt covering the area. Useful for intersection tests.
        // Not an extension method: IGridProvider data is not needed for this pure computation.
        public static BoundsInt GetAreaBounds(Vector2Int origin, Vector2Int size)
            => new BoundsInt(
                new Vector3Int(origin.x, origin.y, 0),
                new Vector3Int(size.x,   size.y,   1));

    }
}
