using UnityEngine;

namespace StrategyGame.Data
{
    // Data for buildings that can produce units (e.g. Barracks).
    [CreateAssetMenu(fileName = "ProductionBuildingData", menuName = "StrategyGame/Data/Production Building Data")]
    public class ProductionBuildingData : BuildingData
    {
        //-------Public Variables-------//
        public UnitData[] ProducibleUnits => _producibleUnits;

        //------Serialized Fields-------//
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
