using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;
using StrategyGame.Grid;

namespace StrategyGame.Units
{
    // Listens to UnitProductionRequestedEvent and spawns the correct unit prefab.
    //
    // Spawn cell selection (BFS):
    //   1. Start from the cell closest to the producer's SpawnWorldPosition.
    //   2. Expand outward (4-directional BFS) until a walkable, unit-free cell is found.
    //   3. If the entire grid is occupied, log a warning and abort.
    //
    // IGridProvider is injected via Inject() (DIP). UnitSpawnSystem only reads the grid
    // (BFS search, coordinate conversion), so IGridProvider is sufficient (ISP).
    // GameBootstrapper is the sole caller of Inject() — it is the composition root.
    //
    // This component should live on a persistent scene GameObject (e.g. "Systems").
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
            EventBus<UnitProductionRequestedEvent>.Subscribe(HandleUnitProductionRequested);
        }

        private void OnDisable()
        {
            EventBus<UnitProductionRequestedEvent>.Unsubscribe(HandleUnitProductionRequested);
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
            EventBus<UnitSpawnedEvent>.Publish(new UnitSpawnedEvent(go));
        }

        #endregion
    }
}
