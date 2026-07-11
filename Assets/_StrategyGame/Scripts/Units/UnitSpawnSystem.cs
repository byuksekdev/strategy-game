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
    // This component should live on a persistent scene GameObject (e.g. "Systems").
    public class UnitSpawnSystem : MonoBehaviour
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//
        [Tooltip("Optional parent transform for spawned units (keeps the hierarchy tidy).")]
        [SerializeField] private Transform _unitParent;

        //------Private Variables-------//

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

            IGridService grid = GridManager.Instance;
            if (grid == null)
            {
                Debug.LogWarning("[UnitSpawnSystem] GridManager not found in scene.");
                return;
            }

            Vector2Int startCell = grid.WorldToGrid(producer.SpawnWorldPosition);
            Vector2Int? spawnCell = FindNearestFreeCell(startCell, grid);

            if (!spawnCell.HasValue)
            {
                Debug.Log("[UnitSpawnSystem] No free cell available for unit spawn — grid is full.");
                return;
            }

            SpawnUnit(unitData, spawnCell.Value, grid);
        }

        // BFS from startCell to find the nearest cell that is both grid-walkable
        // and not already occupied by a live unit.
        private static Vector2Int? FindNearestFreeCell(Vector2Int startCell, IGridService grid)
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

        private void SpawnUnit(UnitData unitData, Vector2Int cell, IGridService grid)
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

            unit.Initialize(unitData, cell);
            EventBus<UnitSpawnedEvent>.Publish(new UnitSpawnedEvent(go));
        }

        #endregion
    }
}
