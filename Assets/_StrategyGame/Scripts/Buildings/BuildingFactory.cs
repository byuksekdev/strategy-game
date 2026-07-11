using Lean.Pool;
using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;
using StrategyGame.Grid;

namespace StrategyGame.Buildings
{
    // Concrete implementation of IBuildingFactory.
    // Creates and places buildings on the grid from BuildingData ScriptableObjects.
    // Selects the correct prefab via BuildingData.Prefab, spawns it through LeanPool,
    // occupies the grid area, and wires runtime data via BuildingBase.Initialize().
    //
    // IGridService is injected via the constructor (DIP):
    //   consumers depend on IBuildingFactory, not this class,
    //   and this class depends on IGridService, not GridManager.
    public class BuildingFactory : IBuildingFactory
    {
        private readonly IGridService _grid;

        public BuildingFactory(IGridService grid)
        {
            _grid = grid;
        }

        // Returns the initialized BuildingBase, or null if placement failed.
        public BuildingBase Create(BuildingData data, Vector2Int gridOrigin, Transform parent = null)
        {
            if (data == null)
            {
                Debug.LogWarning("[BuildingFactory] BuildingData is null.");
                return null;
            }

            if (data.Prefab == null)
            {
                Debug.LogWarning($"[BuildingFactory] '{data.DisplayName}' has no prefab assigned.");
                return null;
            }

            if (_grid == null)
            {
                Debug.LogWarning("[BuildingFactory] IGridService dependency is null.");
                return null;
            }

            Vector3 worldCenter = _grid.GetAreaWorldCenter(gridOrigin, data.Size);
            BuildingBase building = SpawnBuilding(data, worldCenter, parent);
            if (building == null) return null;

            if (!_grid.TryOccupyArea(gridOrigin, data.Size, building.gameObject))
            {
                Debug.Log($"[BuildingFactory] Cannot place '{data.DisplayName}' at {gridOrigin}: area is occupied.");
                LeanPool.Despawn(building.gameObject);
                return null;
            }

            building.Initialize(data, gridOrigin, _grid);
            EventBus<BuildingPlacedEvent>.Publish(new BuildingPlacedEvent(building.gameObject));
            return building;
        }

        // ──────────────────────────────────────────────────────────────────────

        // Spawns the prefab from the pool and retrieves the BuildingBase component.
        // Despawns and returns null if the prefab is missing the required component.
        private static BuildingBase SpawnBuilding(BuildingData data, Vector3 worldPos, Transform parent)
        {
            GameObject go = LeanPool.Spawn(data.Prefab, worldPos, Quaternion.identity, parent);
            go.name = data.DisplayName;

            BuildingBase building = go.GetComponent<BuildingBase>();
            if (building != null) return building;

            Debug.LogWarning($"[BuildingFactory] '{data.Prefab.name}' has no BuildingBase component.");
            LeanPool.Despawn(go);
            return null;
        }
    }
}
