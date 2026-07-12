using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame.Grid
{
    // Read-only abstraction for grid data access and observation.
    // Placement, pathfinding and any read/react system depends on this instead of GridManager directly.
    // Write operations (occupy/free) live in IGridOccupancyManager.
    public interface IGridProvider
    {
        int Width { get; }
        int Height { get; }
        float CellSize { get; }

        GridCell GetCell(int x, int y);
        GridCell GetCell(Vector2Int coordinate);

        bool IsValidCoordinate(int x, int y);
        bool IsValidCoordinate(Vector2Int coordinate);

        Vector3 GridToWorld(Vector2Int coordinate);
        Vector2Int WorldToGrid(Vector3 worldPosition);

        // includeDiagonals: true returns up to 8 neighbours instead of 4.
        IEnumerable<GridCell> GetNeighbors(GridCell cell, bool includeDiagonals = false);

        // Returns true when every cell in the area is inside bounds and walkable.
        bool IsAreaFree(Vector2Int origin, Vector2Int size);

        // Returns the world-space centre of a multi-cell area.
        // Used by BuildingFactory to correctly position buildings that span multiple cells.
        Vector3 GetAreaWorldCenter(Vector2Int origin, Vector2Int size);
    }
}
