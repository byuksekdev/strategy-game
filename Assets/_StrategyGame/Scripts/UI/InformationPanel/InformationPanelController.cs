using UnityEngine;
using UnityEngine.UI;
using StrategyGame.Buildings;
using StrategyGame.Core;
using StrategyGame.Data;

namespace StrategyGame.UI.InformationPanel
{
    // Manages the information panel (MVC — Controller layer).
    //
    // Three selection scenarios:
    //   1. Building selection from the left production menu → Building name + icon is displayed.
    //                                               Building is not yet placed, so delete/production list is not visible.
    //   2. Building selection from the grid → Building name + icon + HP + production list (if IUnitProducer)
    //                                               + delete button is displayed.
    //   3. Clicking on an empty area in the grid or the X button → Panel closes.
    //
    // InformationPanelController listens to events; the sub-views (EntityInfoView, UnitListController)
    // are only visual and directed by this controller.
    public class InformationPanelController : MonoBehaviour
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//
        [Header("Panel Root")]
        [Tooltip("The root GameObject of the panel to be shown/hidden.")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Sub-Views")]
        [Tooltip("The sub-view that displays the entity name, icon, and HP.")]
        [SerializeField] private EntityInfoView _entityInfoView;

        [Tooltip("The controller that manages the production list when a production building is selected.")]
        [SerializeField] private UnitListController _unitListController;

        [Header("Buttons")]
        [Tooltip("The button that closes the panel.")]
        [SerializeField] private Button _closeButton;

        [Tooltip("The button that deletes the selected building from the grid. Only visible when a grid selection is made.")]
        [SerializeField] private Button _deleteButton;

        //------Private Variables-------//
        // Stores the selected building from the grid; null in menu selection.
        // DeleteSelectedBuilding() uses this reference.
        private BuildingBase _selectedBuilding;

        #region UNITY_METHODS

        private void Awake()
        {
            _panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnSelectionChanged += HandleGridSelection;
            GameEvents.OnBuildingProductionRequested += HandleMenuSelection;
            GameEvents.OnPlacementModeExited += HandlePlacementExited;
            GameEvents.OnBuildingDestroyed += HandleBuildingDestroyed;
            GameEvents.OnHPChanged += HandleHPChanged;

            _closeButton.onClick.AddListener(ClosePanel);
            _deleteButton.onClick.AddListener(DeleteSelectedBuilding);
        }

        private void OnDisable()
        {
            GameEvents.OnSelectionChanged -= HandleGridSelection;
            GameEvents.OnBuildingProductionRequested -= HandleMenuSelection;
            GameEvents.OnPlacementModeExited -= HandlePlacementExited;
            GameEvents.OnBuildingDestroyed -= HandleBuildingDestroyed;
            GameEvents.OnHPChanged -= HandleHPChanged;

            _closeButton.onClick.RemoveListener(ClosePanel);
            _deleteButton.onClick.RemoveListener(DeleteSelectedBuilding);
        }

        #endregion

        #region PUBLIC_METHODS

        // X button is clicked; closes the panel and informs the system that the selection has been removed.
        public void ClosePanel()
        {
            _selectedBuilding = null;
            _unitListController.Hide();
            _entityInfoView.Clear();
            _panelRoot.SetActive(false);

            // Informs the listeners that the selection has been removed (highlight systems, etc.).
            GameEvents.SelectionChanged(null);
        }

        #endregion

        #region PRIVATE_METHODS

        // Building card is clicked from the left production menu; closes the panel and informs the system that the selection has been removed.
        // Building is not yet placed, so delete button and production list are not visible.
        private void HandleMenuSelection(BuildingData buildingData)
        {
            _selectedBuilding = null;

            _entityInfoView.Bind(buildingData);
            _unitListController.Hide();
            _deleteButton.gameObject.SetActive(false);

            _panelRoot.SetActive(true);
        }

        // Grid selection is made or the selection is removed; calls HidePanel().
        // If selectable is null, the panel is closed.
        private void HandleGridSelection(ISelectable selectable)
        {
            if (selectable == null)
            {
                HidePanel();
                return;
            }

            if (selectable is BuildingBase building)
            {
                ShowForBuilding(building);
                return;
            }

            HidePanel();
        }

        // Shows the selected building in the panel with full information.
        private void ShowForBuilding(BuildingBase building)
        {
            _selectedBuilding = building;

            _entityInfoView.Bind(building.BuildingData, building);
            _deleteButton.gameObject.SetActive(true);

            if (building is IUnitProducer producer && producer.CanProduceUnits)
                _unitListController.ShowForProducer(producer);
            else
                _unitListController.Hide();

            _panelRoot.SetActive(true);
        }

        // Placement mode ended (building placed or cancelled):
        // If there is a menu selection displayed in the panel (not a grid selection), close the panel.
        private void HandlePlacementExited()
        {
            if (_selectedBuilding == null)
                HidePanel();
        }

        // Selected building is destroyed for an external reason (battle damage, etc.): close the panel.
        private void HandleBuildingDestroyed(GameObject destroyedGo)
        {
            if (_selectedBuilding != null && _selectedBuilding.gameObject == destroyedGo)
                HidePanel();
        }

        // Selected building HP changed; updates the HP indicator in the EntityInfoView.
        private void HandleHPChanged(IDamageable damageable)
        {
            if (_selectedBuilding == null) return;
            if (damageable is BuildingBase building && building == _selectedBuilding)
                _entityInfoView.RefreshHP(damageable);
        }

        // Delete button is clicked; deletes the selected building from the grid.
        private void DeleteSelectedBuilding()
        {
            if (_selectedBuilding == null) return;

            BuildingBase toDelete = _selectedBuilding;
            HidePanel(); // Clear the reference; then trigger the BuildingDestroyed event.
            toDelete.Die();
        }

        // Closes the panel without publishing any events.
        // Used when SelectionChanged(null) is triggered from outside.
        private void HidePanel()
        {
            _selectedBuilding = null;
            _unitListController.Hide();
            _entityInfoView.Clear();
            _panelRoot.SetActive(false);
        }

        #endregion
    }
}
