using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;
using StrategyGame.Grid;

namespace StrategyGame.Units
{
    // Manages the full unit lifecycle: spawn → UnitRegistry registration → destruction → unregistration.
    //
    // SRP: UnitSpawnSystem is the single owner of UnitRegistry write operations for lifecycle events.
    //   • SpawnUnit()          → unit.Initialize() then UnitRegistry.Register()
    //   • HandleUnitDestroyed  → UnitRegistry.Unregister() on UnitDestroyedEvent
    //   Mid-movement registry updates (register/unregister per step) remain in MoveCoroutine
    //   because they are inherently coupled to movement state.
    //
    // Spawn cell selection (BFS):
    //   1. Start from the cell closest to the producer's SpawnWorldPosition.
    //   2. Expand outward (4-directional BFS) until a walkable, unit-free cell is found.
    //   3. If the entire grid is occupied, log a warning and abort.
    //
    // IGridProvider is injected via Inject() (DIP). UnitSpawnSystem only reads the grid
    // (BFS search, coordinate conversion), so IGridProvider is sufficient (ISP).
    // GameBootstrapper is the sole caller of Inject() — it is the composition root.
    public class UnitSpawnSystem : MonoBehaviour
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//
        [Tooltip("Optional parent transform for spawned units (keeps the hierarchy tidy).")]
        [SerializeField] private Transform _unitParent;

        //------Private Variables-------//
        private IGridProvider _grid;

        #region UNITY_METHODS

        private void OnEnable()
        {
            EventBus.Subscribe<UnitProductionRequestedEvent>(HandleUnitProductionRequested);
            EventBus.Subscribe<UnitDestroyedEvent>(HandleUnitDestroyed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<UnitProductionRequestedEvent>(HandleUnitProductionRequested);
            EventBus.Unsubscribe<UnitDestroyedEvent>(HandleUnitDestroyed);
        }

        #endregion

        #region PUBLIC_METHODS

        // Called by GameBootstrapper to inject the grid read dependency.
        public void Inject(IGridProvider grid)
        {
            _grid = grid;
        }

        #endregion

        #region PRIVATE_METHODS

        // Unregisters the unit from the registry when it is destroyed.
        // UnitDestroyedEvent carries the GridPosition so UnitBase.Die() does not need
        // a direct UnitRegistry dependency (SRP). EventBus is synchronous, so this runs
        // before LeanPool.Despawn in the same call stack.
        private static void HandleUnitDestroyed(UnitDestroyedEvent e)
        {
            UnitRegistry.Unregister(e.GridPosition);
        }

        private void HandleUnitProductionRequested(UnitProductionRequestedEvent e)
        {
            UnitData unitData = e.UnitData;
            IUnitProducer producer = e.Producer;

            if (unitData == null || unitData.Prefab == null)
            {
                Debug.LogWarning("[UnitSpawnSystem] UnitData or its Prefab is null.");
                return;
            }

            if (_grid == null)
            {
                Debug.LogWarning("[UnitSpawnSystem] IGridProvider dependency is null — call Inject() first.");
                return;
            }

            Vector2Int startCell = _grid.WorldToGrid(producer.SpawnWorldPosition);
            Vector2Int? spawnCell = FindNearestFreeCell(startCell, _grid);

            if (!spawnCell.HasValue)
            {
                Debug.Log("[UnitSpawnSystem] No free cell available for unit spawn — grid is full.");
                return;
            }

            SpawnUnit(unitData, spawnCell.Value, _grid);
        }

        // BFS from startCell to find the nearest cell that is both grid-walkable
        // and not already occupied by a live unit.
        private static Vector2Int? FindNearestFreeCell(Vector2Int startCell, IGridProvider grid)
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();

            queue.Enqueue(startCell);
            visited.Add(startCell);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                GridCell cell = grid.GetCell(current);

                if (cell != null && cell.IsWalkable && !UnitRegistry.IsCellOccupied(current))
                    return current;

                if (cell == null) continue;

                foreach (GridCell neighbour in grid.GetNeighbors(cell))
                {
                    if (visited.Contains(neighbour.Coordinate)) continue;
                    visited.Add(neighbour.Coordinate);
                    queue.Enqueue(neighbour.Coordinate);
                }
            }

            return null;
        }

        private void SpawnUnit(UnitData unitData, Vector2Int cell, IGridProvider grid)
        {
            Vector3 worldPos = grid.GridToWorld(cell);
            GameObject go = LeanPool.Spawn(unitData.Prefab, worldPos, Quaternion.identity, _unitParent);
            go.name = unitData.DisplayName;

            UnitBase unit = go.GetComponent<UnitBase>();
            if (unit == null)
            {
                Debug.LogWarning($"[UnitSpawnSystem] Prefab '{unitData.Prefab.name}' has no UnitBase component.");
                LeanPool.Despawn(go);
                return;
            }

            unit.Initialize(unitData, cell, grid);
            // Register after Initialize() so _gridPosition is set inside the unit.
            // UnitSpawnSystem owns registry lifecycle (SRP); UnitBase.Initialize() does not.
            UnitRegistry.Register(unit, cell);
        }

        #endregion
    }
}
