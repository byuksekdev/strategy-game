using UnityEngine;
using UnityEngine.UI;
using StrategyGame.Core;
using StrategyGame.Data;

namespace StrategyGame.UI.InformationPanel
{
    // Manages the information panel (MVC — Controller layer).
    //
    // Four selection scenarios:
    //   1. Building card clicked from the left production menu
    //        → Name + icon; delete button and production list hidden.
    //   2. Grid entity selected (building, unit, or any future IInfoPanelProvider type)
    //        → Name + icon + HP + delete button (if CanBeDeleted) + production list (if UnitProducer).
    //   3. Empty area clicked, X button pressed, or placement mode ended
    //        → Panel closes.
    //
    // OCP: HandleGridSelection depends on IInfoPanelProvider — not on BuildingBase or UnitBase.
    // Adding a new entity type (Vehicle, Hero, etc.) requires zero changes here; the new type
    // simply implements IInfoPanelProvider and everything wires up automatically.
    //
    // HP updates come from the selected entity's local IDamageable.OnHealthChanged event,
    // not from the global EventBus, to avoid reacting to every hit in the scene.
    //
    // Sub-views (EntityInfoView, UnitListController) are purely visual;
    // this controller orchestrates them.
    public class InformationPanelController : MonoBehaviour
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//
        [Header("Panel Root")]
        [Tooltip("The root GameObject of the panel to be shown/hidden.")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Sub-Views")]
        [Tooltip("Displays entity name, icon, and HP.")]
        [SerializeField] private EntityInfoView _entityInfoView;

        [Tooltip("Manages the production list when a production building is selected.")]
        [SerializeField] private UnitListController _unitListController;

        [Header("Buttons")]
        [Tooltip("Closes the panel.")]
        [SerializeField] private Button _closeButton;

        [Tooltip("Deletes the selected building from the grid. Visible only when CanBeDeleted is true.")]
        [SerializeField] private Button _deleteButton;

        //------Private Variables-------//
        // Non-null while a grid entity is selected. Used for HP event subscription and deletion.
        // Typed as IDamageable so this controller has no dependency on concrete entity classes.
        private IDamageable _selectedDamageable;

        #region UNITY_METHODS

        private void Awake()
        {
            _panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus<SelectionChangedEvent>.Subscribe(HandleGridSelection);
            EventBus<BuildingProductionRequestedEvent>.Subscribe(HandleMenuSelection);
            EventBus<PlacementModeExitedEvent>.Subscribe(HandlePlacementExited);
            EventBus<BuildingDestroyedEvent>.Subscribe(HandleBuildingDestroyed);
            EventBus<UnitDestroyedEvent>.Subscribe(HandleUnitDestroyed);

            _closeButton.onClick.AddListener(ClosePanel);
            _deleteButton.onClick.AddListener(DeleteSelectedEntity);
        }

        private void OnDisable()
        {
            EventBus<SelectionChangedEvent>.Unsubscribe(HandleGridSelection);
            EventBus<BuildingProductionRequestedEvent>.Unsubscribe(HandleMenuSelection);
            EventBus<PlacementModeExitedEvent>.Unsubscribe(HandlePlacementExited);
            EventBus<BuildingDestroyedEvent>.Unsubscribe(HandleBuildingDestroyed);
            EventBus<UnitDestroyedEvent>.Unsubscribe(HandleUnitDestroyed);

            _closeButton.onClick.RemoveListener(ClosePanel);
            _deleteButton.onClick.RemoveListener(DeleteSelectedEntity);
        }

        #endregion

        #region PUBLIC_METHODS

        // X button pressed: closes the panel and broadcasts SelectionChangedEvent(null)
        // so highlight systems and SelectionController can react.
        public void ClosePanel()
        {
            HidePanel();
            EventBus<SelectionChangedEvent>.Publish(new SelectionChangedEvent(null));
        }

        #endregion

        #region PRIVATE_METHODS

        // Building card clicked from the left production menu.
        // Building not yet placed → no delete button, no production list, no HP.
        private void HandleMenuSelection(BuildingProductionRequestedEvent e)
        {
            DetachHealthListener();

            _entityInfoView.Bind(e.BuildingData);
            _unitListController.Hide();
            _deleteButton.gameObject.SetActive(false);

            _panelRoot.SetActive(true);
        }

        // A grid entity was selected (or selection was cleared).
        //
        // OCP-compliant: dispatches on IInfoPanelProvider, not on concrete types.
        // Any class that implements IInfoPanelProvider is handled here without modification.
        private void HandleGridSelection(SelectionChangedEvent e)
        {
            if (e.Selected == null)
            {
                HidePanel();
                return;
            }

            if (e.Selected is IInfoPanelProvider provider)
            {
                ShowForEntity(provider);
                return;
            }

            HidePanel();
        }

        // Populates and shows the panel for any IInfoPanelProvider entity.
        // Replaces the previous ShowForBuilding / ShowForUnit pair.
        private void ShowForEntity(IInfoPanelProvider provider)
        {
            DetachHealthListener();

            // Both BuildingBase and UnitBase implement IDamageable; safe for any IInfoPanelProvider.
            _selectedDamageable = provider as IDamageable;
            if (_selectedDamageable != null)
                _selectedDamageable.OnHealthChanged += HandleSelectedEntityHealthChanged;

            _entityInfoView.Bind(provider.EntityData, _selectedDamageable);
            _deleteButton.gameObject.SetActive(provider.CanBeDeleted);

            IUnitProducer producer = provider.UnitProducer;
            if (producer != null && producer.CanProduceUnits)
                _unitListController.ShowForProducer(producer);
            else
                _unitListController.Hide();

            _panelRoot.SetActive(true);
        }

        // Placement mode ended; close the panel if only a menu selection is showing
        // (no grid entity selected → _selectedDamageable is null).
        private void HandlePlacementExited(PlacementModeExitedEvent e)
        {
            if (_selectedDamageable == null)
                HidePanel();
        }

        // An entity was destroyed externally: close if it is the currently selected one.
        private void HandleBuildingDestroyed(BuildingDestroyedEvent e)
        {
            if (_selectedDamageable is MonoBehaviour mb && mb.gameObject == e.Building)
                HidePanel();
        }

        private void HandleUnitDestroyed(UnitDestroyedEvent e)
        {
            if (_selectedDamageable is MonoBehaviour mb && mb.gameObject == e.Unit)
                HidePanel();
        }

        // HP changed on the currently selected entity: refresh the HP display.
        private void HandleSelectedEntityHealthChanged(int currentHP, int maxHP)
        {
            _entityInfoView.RefreshHP(_selectedDamageable);
        }

        // Closes the panel silently (no SelectionChangedEvent broadcast).
        // Used when the broadcast already came from an external source.
        private void HidePanel()
        {
            DetachHealthListener();
            _unitListController.Hide();
            _entityInfoView.Clear();
            _panelRoot.SetActive(false);
        }

        // Unsubscribes from the previously selected entity's local health event
        // and clears the selection reference.
        private void DetachHealthListener()
        {
            if (_selectedDamageable != null)
            {
                _selectedDamageable.OnHealthChanged -= HandleSelectedEntityHealthChanged;
                _selectedDamageable = null;
            }
        }

        // Delete button pressed: destroy the currently selected entity via IDamageable.Die().
        // The button is only visible when provider.CanBeDeleted is true (i.e. grid-placed buildings).
        private void DeleteSelectedEntity()
        {
            if (_selectedDamageable == null) return;

            IDamageable toDelete = _selectedDamageable;
            HidePanel();
            toDelete.Die();
        }

        #endregion
    }
}
