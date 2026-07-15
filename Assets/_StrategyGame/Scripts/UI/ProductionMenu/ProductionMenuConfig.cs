using System.Collections.Generic;
using UnityEngine;
using StrategyGame.Data;

namespace StrategyGame.UI.ProductionMenu
{
    // Production Menu Config: Defines the producible items listed in the production menu.
    // Stores EntityData (base type) so the menu is open to any future producible kind
    // (buildings, vehicles, heroes…) without code changes — OCP / DIP.
    // Concrete assets (BuildingData, UnitData, …) can be assigned freely in the Inspector.
    [CreateAssetMenu(fileName = "ProductionMenuConfig",
                     menuName  = "StrategyGame/Config/Production Menu Config")]
    public class ProductionMenuConfig : ScriptableObject
    {
        //-------Public Variables-------//
        public List<EntityData> AvailableEntities => _availableEntities;

        //------Serialized Fields-------//
        [Header("Producible items to list in the production menu (drag EntityData assets here)")]
        [SerializeField] private List<EntityData> _availableEntities;

        //------Private Variables-------//

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        #endregion
    }
}
