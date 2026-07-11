using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;

namespace StrategyGame.Buildings
{
    // Barracks: the only building that can produce soldier units.
    // Has a configurable spawn cell offset (relative to GridOrigin) that defines
    // the preferred spawn tile. UnitSpawnSystem performs BFS from this point
    // to find the nearest free cell when units stack up.
    public class Barracks : BuildingBase, IUnitProducer
    {
        //-------Public Variables-------//
        public UnitData[] ProducibleUnits => ProductionData != null
            ? ProductionData.ProducibleUnits
            : System.Array.Empty<UnitData>();

        public bool CanProduceUnits => !IsDead && ProducibleUnits.Length > 0;

        // World-space position of the preferred spawn point.
        // Exposed via IUnitProducer so UnitSpawnSystem can start its BFS here.
        public Vector3 SpawnWorldPosition => _spawnPoint != null
            ? _spawnPoint.position
            : transform.position;

        //------Serialized Fields-------//
        // Offset (in grid cells) from GridOrigin to the preferred spawn cell.
        // Default (1, -1) = 2nd column, one row below the building's bottom edge,
        // which lands at the bottom-middle area of a 4×4 Barracks.
        // Adjust freely in the Inspector to reposition the spawn tile.
        [Tooltip("Grid-cell offset from GridOrigin to the preferred spawn tile. " +
                 "For a 4×4 Barracks the bottom-middle cells are at offsets (1,-1) and (2,-1).")]
        [SerializeField] private Vector2Int _spawnCellOffset = new Vector2Int(1, -1);

        //------Private Variables-------//
        private Transform _spawnPoint;

        // Cast helper: safe null if BuildingData was not set as ProductionBuildingData.
        private ProductionBuildingData ProductionData => BuildingData as ProductionBuildingData;

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        public override void OnSelected()
        {
            base.OnSelected();
        }

        public override void OnDeselected()
        {
            base.OnDeselected();
        }

        #endregion

        #region PRIVATE_METHODS

        protected override void OnInitialized()
        {
            ResolveSpawnPoint();
        }

        // Creates the SpawnPoint Transform and positions it at the configured spawn cell.
        // The desired cell is GridOrigin + _spawnCellOffset.
        // If the coordinate is out of bounds, the spawn point falls back to the building centre.
        private void ResolveSpawnPoint()
        {
            if (_spawnPoint == null)
            {
                _spawnPoint = new GameObject("SpawnPoint").transform;
                _spawnPoint.SetParent(transform);
            }

            if (GridProvider == null)
            {
                _spawnPoint.localPosition = Vector3.zero;
                return;
            }

            Vector2Int desiredCell = GridOrigin + _spawnCellOffset;

            if (GridProvider.IsValidCoordinate(desiredCell))
                _spawnPoint.position = GridProvider.GridToWorld(desiredCell);
            else
                _spawnPoint.localPosition = Vector3.zero;
        }

        #endregion
    }
}
