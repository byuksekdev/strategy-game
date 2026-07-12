using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;

namespace StrategyGame.UI.ProductionMenu
{
    // Manages the production menu on the left panel (MVC — Controller layer).
    // Responsibilities:
    //   1. Injects the building list from ProductionMenuConfig into InfiniteScrollView.
    //   2. Publishes the BuildingProductionRequested event when a building item is clicked.
    public class ProductionMenuController : MonoBehaviour
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//
        [Header("Data")]
        [SerializeField] private ProductionMenuConfig _config;

        [Header("Dependencies")]
        [SerializeField] private InfiniteScrollView _buildingScrollView;

        //------Private Variables-------//

        #region UNITY_METHODS

        private void Awake()
        {
            PopulateBuildingList();
        }

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        private void PopulateBuildingList()
        {
            if (_config == null)
            {
                Debug.LogWarning("[ProductionMenuController] ProductionMenuConfig is not assigned.");
                return;
            }

            if (_buildingScrollView == null)
            {
                Debug.LogWarning("[ProductionMenuController] InfiniteScrollView is not assigned.");
                return;
            }

            _buildingScrollView.Initialize(
                _config.AvailableBuildings, OnBuildingItemClicked);
        }

        // When a building item is clicked, it publishes the BuildingProductionRequestedEvent to the event bus;
        // both the placement system and the information panel react to it.
        private void OnBuildingItemClicked(BuildingData data)
        {
            EventBus.Publish(new BuildingProductionRequestedEvent(data));
        }

        #endregion
    }
}
