using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame.Units
{
    // Lightweight static registry that maps grid coordinates to live units.
    // Used by UnitSpawnSystem (BFS spawn) and SelectionController (click detection)
    // to locate units without relying on Physics2D or GridCell occupancy.
    //
    // Thread-safety: not required — Unity is single-threaded.
    // Lifecycle: call Clear() on scene unload (e.g. from a GameBootstrap).
    public static class UnitRegistry
    {
        //------Private Variables-------//
        private static readonly Dictionary<Vector2Int, UnitBase> _unitsByCell = new();

        #region PUBLIC_METHODS

        // Registers a unit at the given cell. Overwrites any stale entry.
        public static void Register(UnitBase unit, Vector2Int cell)
        {
            if (unit == null) return;
            _unitsByCell[cell] = unit;
        }

        // Removes the registration for the given cell.
        // Safe to call even when the cell is not registered.
        public static void Unregister(Vector2Int cell)
            => _unitsByCell.Remove(cell);

        public static bool IsCellOccupied(Vector2Int cell)
            => _unitsByCell.ContainsKey(cell);

        // Returns the unit at the given cell, or null if none.
        public static UnitBase GetUnitAt(Vector2Int cell)
            => _unitsByCell.TryGetValue(cell, out UnitBase unit) ? unit : null;

        // Clears all registrations. Call on scene transitions.
        public static void Clear()
            => _unitsByCell.Clear();

        #endregion
    }
}
