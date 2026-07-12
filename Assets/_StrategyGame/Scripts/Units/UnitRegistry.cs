using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame.Units
{
    // Lightweight static registry that maps grid coordinates to live units.
    // Used by UnitSpawnSystem (BFS spawn) and SelectionController (click detection)
    // to locate units without relying on Physics2D or GridCell occupancy.
    //
    // Thread-safety: not required — Unity is single-threaded.
    // Lifecycle: automatically cleared on domain reload via [RuntimeInitializeOnLoadMethod].
    //            Also cleared by GameBootstrapper.OnDestroy for runtime scene reloads.
    public static class UnitRegistry
    {
        //------Private Variables-------//
        private static readonly Dictionary<Vector2Int, UnitBase> _unitsByCell = new();

        // Runs before any scene loads AND whenever the domain reloads (e.g. entering Play Mode
        // with "Enter Play Mode Options / Reload Domain" disabled in Project Settings).
        // This guarantees a clean slate on every play session without coupling callers to Clear().
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload() => _unitsByCell.Clear();

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
