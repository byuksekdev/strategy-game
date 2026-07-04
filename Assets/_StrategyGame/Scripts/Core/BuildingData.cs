using UnityEngine;

namespace StrategyGame.Data
{
    // Data for all buildings (Barracks, Power Plant, etc.).
    [CreateAssetMenu(fileName = "BuildingData", menuName = "StrategyGame/Data/Building Data")]
    public class BuildingData : ProducibleData
    {
        //-------Public Variables-------//
        public BuildingType BuildingType => _buildingType;
        public int MaxHP => _maxHP;

        public Vector2Int Size => _size;
        public GameObject Prefab => _prefab;

        // Units that can be produced from this building. If empty, the production menu is not shown.
        public UnitData[] ProducibleUnits => _producibleUnits;

        public bool CanProduceUnits => _producibleUnits != null && _producibleUnits.Length > 0;

        //------Serialized Fields-------//
        [Header("Type")]
        [SerializeField] private BuildingType _buildingType;

        [Header("Stats")]
        [SerializeField] private int _maxHP = 100;

        [Header("Grid Size")]
        [SerializeField] private Vector2Int _size = Vector2Int.one;

        [Header("Prefab")]
        [SerializeField] private GameObject _prefab;

        [Header("Producible Units")]
        [SerializeField] private UnitData[] _producibleUnits;

        //------Private Variables-------//

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        #endregion


    }
}
