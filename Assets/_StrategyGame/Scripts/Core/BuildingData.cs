using UnityEngine;

namespace StrategyGame.Data
{
    // Data for all buildings (Barracks, Power Plant, etc.).
    [CreateAssetMenu(fileName = "BuildingData", menuName = "StrategyGame/Data/Building Data")]
    public class BuildingData : EntityData
    {
        //-------Public Variables-------//
        public BuildingType BuildingType => _buildingType;
        public int MaxHP => _maxHP;

        public Vector2Int Size => _size;
        public GameObject Prefab => _prefab;

        //------Serialized Fields-------//
        [Header("Type")]
        [SerializeField] private BuildingType _buildingType;

        [Header("Stats")]
        [SerializeField] private int _maxHP = 100;

        [Header("Grid Size")]
        [SerializeField] private Vector2Int _size = Vector2Int.one;

        [Header("Prefab")]
        [SerializeField] private GameObject _prefab;

        //------Private Variables-------//

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        #endregion


    }
}
