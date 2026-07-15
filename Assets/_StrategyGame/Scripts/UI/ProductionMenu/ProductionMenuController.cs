using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;

namespace StrategyGame.UI.ProductionMenu
{
    // Manages the production menu on the left panel (MVC — Controller layer).
    //
    // Responsibilities:
    //   1. Feeds ProductionMenuConfig.AvailableEntities (List<EntityData> / IProducible) into
    //      InfiniteScrollView — the scroll view is fully generic; it never sees BuildingData.
    //   2. On click, casts EntityData to BuildingData and publishes BuildingProductionRequestedEvent.
    //      The cast is intentional: the production menu only ever lists buildings today,
    //      and BuildingPlacementController requires BuildingData for grid/ghost operations.
    //      If future entity types need different events, add a dispatch method here (OCP).
    //
    // DIP: this class depends on IProducible / EntityData abstractions, not on BuildingData directly.
    // OCP: adding a new producible kind only requires a new event dispatch branch, not a rewrite.
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

        private void Start()
        {
            PopulateProductionList();
        }

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        private void PopulateProductionList()
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

            // Pass the generic IProducible list; InfiniteScrollView does not know about BuildingData.
            _buildingScrollView.Initialize(_config.AvailableEntities, OnProducibleItemClicked);
        }

        // Dispatches the appropriate event for the clicked IProducible item.
        // Currently only BuildingData is supported; extend here for future entity kinds.
        private void OnProducibleItemClicked(EntityData data)
        {
            if (data is BuildingData buildingData)
                EventBus.Publish(new BuildingProductionRequestedEvent(buildingData));
            else
                Debug.LogWarning($"[ProductionMenuController] No handler for producible type: {data?.GetType().Name}");
        }

        #endregion
    }
}
