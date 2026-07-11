using UnityEngine;
using StrategyGame.Data;

namespace StrategyGame.Core
{
    // Implemented by buildings that can produce units (e.g. Barracks).
    public interface IUnitProducer
    {
        UnitData[] ProducibleUnits { get; }
        bool CanProduceUnits { get; }

        // World-space position where produced units should emerge.
        // UnitSpawnSystem uses this as the BFS start point to find the nearest free cell.
        Vector3 SpawnWorldPosition { get; }
    }
}
