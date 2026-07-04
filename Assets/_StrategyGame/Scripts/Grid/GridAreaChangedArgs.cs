using UnityEngine;

namespace StrategyGame.Grid
{
    // Immutable value type passed with IGridProvider.OnAreaOccupancyChanged.
    // Readonly struct: no heap allocation, named fields, easily extensible without breaking subscribers.
    public readonly struct GridAreaChangedArgs
    {
        public Vector2Int Origin { get; }  // Bottom-left cell of the affected area.
        public Vector2Int Size { get; }  // Width and height in grid cells.
        public bool IsOccupied { get; }  // true = building placed, false = building destroyed.

        public GridAreaChangedArgs(Vector2Int origin, Vector2Int size, bool isOccupied)
        {
            Origin = origin;
            Size = size;
            IsOccupied = isOccupied;
        }

        public override string ToString()
            => $"GridAreaChanged(origin={Origin}, size={Size}, occupied={IsOccupied})";

    }
}
