using System.Collections.Generic;
using UnityEngine;
using StrategyGame.Data;

namespace StrategyGame.UI.ProductionMenu
{
    // Production Menu Config: Defines the buildings listed in the production menu.
    // Filled in the Inspector; new building types can be added here without code changes.
    [CreateAssetMenu(fileName = "ProductionMenuConfig",
                     menuName  = "StrategyGame/Config/Production Menu Config")]
    public class ProductionMenuConfig : ScriptableObject
    {
        //-------Public Variables-------//
        public List<BuildingData> AvailableBuildings => _availableBuildings;

        //------Serialized Fields-------//
        [Header("Buildings to be listed in the production menu (order, display order)")]
        [SerializeField] private List<BuildingData> _availableBuildings;

        //------Private Variables-------//

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        #endregion
    }
}
