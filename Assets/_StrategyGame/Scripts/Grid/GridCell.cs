using UnityEngine;

namespace StrategyGame.Grid
{
    // Represents a single tile in the game grid.
    // Pure data class — no MonoBehaviour overhead.
    // A cell is walkable only when no building occupies it.
    public class GridCell
    {
        //-------Public Variables-------//
        public Vector2Int Coordinate { get; }
        public Vector3 WorldPosition { get; }
        public bool IsOccupied => _occupant != null;
        public bool IsWalkable => _occupant == null;
        public GameObject Occupant => _occupant;

        //------Private Variables-------//
        private GameObject _occupant;

        #region CONSTRUCTOR

        public GridCell(Vector2Int coordinate, Vector3 worldPosition)
        {
            Coordinate = coordinate;
            WorldPosition = worldPosition;
        }

        #endregion

        #region PUBLIC_METHODS

        // Pass null to clear the occupant. Internal: only GridManager should mutate cell state.
        internal void SetOccupant(GameObject occupant) => _occupant = occupant;

        public override string ToString() => $"Cell({Coordinate.x}, {Coordinate.y})";

        #endregion
    }
}
