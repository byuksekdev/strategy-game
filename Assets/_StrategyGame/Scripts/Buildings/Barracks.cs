using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;
using StrategyGame.Grid;

namespace StrategyGame.Buildings
{
    // Barracks: the only building that can produce soldier units.
    // Holds a designated SpawnPoint where newly created units will appear.
    // Unit production logic lives in the Production system; Barracks only stores the data contract.
    public class Barracks : BuildingBase, IUnitProducer
    {
        //-------Public Variables-------//
        public UnitData[] ProducibleUnits => ProductionData != null
            ? ProductionData.ProducibleUnits
            : System.Array.Empty<UnitData>();

        public bool CanProduceUnits => !IsDead && ProducibleUnits.Length > 0;

        // World-space position where produced units will spawn next to this Barracks.
        public Transform SpawnPoint => _spawnPoint;

        //------Serialized Fields-------//
        // Can be pre-assigned in the prefab Inspector. If left null, resolved at runtime via grid.
        [SerializeField] private Transform _spawnPoint;

        //------Private Variables-------//
        // Cast helper: safe null if BuildingData was not set as ProductionBuildingData.
        private ProductionBuildingData ProductionData => BuildingData as ProductionBuildingData;

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        public override void OnSelected()
        {
            base.OnSelected();
            // Barracks-specific selection: the Info Panel also lists ProducibleUnits via IUnitProducer.
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

        // Finds or creates the SpawnPoint Transform and positions it at the first walkable cell
        // adjacent to this building's grid footprint.
        private void ResolveSpawnPoint()
        {
            if (_spawnPoint == null)
            {
                _spawnPoint = new GameObject("SpawnPoint").transform;
                _spawnPoint.SetParent(transform);
            }

            if (GridManager.Instance == null) return;

            GridCell adjacent = GridManager.Instance.FindFirstFreeAdjacentCell(GridOrigin, BuildingData.Size);
            if (adjacent != null)
                _spawnPoint.position = adjacent.WorldPosition;
            else
                _spawnPoint.localPosition = Vector3.zero; // Fallback: spawn at building center
        }

        #endregion
    }
}
