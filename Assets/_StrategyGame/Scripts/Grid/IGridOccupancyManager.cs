using UnityEngine;

namespace StrategyGame.Grid
{
    // Write-side abstraction for the game grid.
    // ISP: pathfinding only needs IGridProvider; placement systems need both interfaces.
    public interface IGridOccupancyManager
    {
        // if any cell is blocked or out of bounds, returns false and no cells are modified.
        bool TryOccupyArea(Vector2Int origin, Vector2Int size, GameObject occupant);

        // Safe to call on partially-occupied or already-free regions. Call when a building is destroyed.
        void FreeArea(Vector2Int origin, Vector2Int size);
    }
}
