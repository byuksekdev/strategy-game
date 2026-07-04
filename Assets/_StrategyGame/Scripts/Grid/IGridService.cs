namespace StrategyGame.Grid
{
    // Convenience interface for systems that need full grid access (read + write).
    // Pathfinding and visualizers should prefer IGridProvider.
    // Placement and destruction systems that only write should prefer IGridOccupancyManager.
    // Use this only when both capabilities are genuinely required.
    public interface IGridService : IGridProvider, IGridOccupancyManager { }
}
