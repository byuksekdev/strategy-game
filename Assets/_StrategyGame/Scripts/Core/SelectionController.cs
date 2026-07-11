using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using StrategyGame.Buildings;
using StrategyGame.Grid;
using StrategyGame.Pathfinding;
using StrategyGame.Units;

namespace StrategyGame.Core
{
    // Handles all mouse-driven selection, movement, and attack commands.
    //
    // Left click (not over UI, not in placement mode):
    //   → WorldToGrid → check GridCell.Occupant (buildings) OR UnitRegistry (units)
    //       found → OnSelected() + EventBus<SelectionChangedEvent>.Publish(entity)
    //       empty → ClearSelection() + EventBus<SelectionChangedEvent>.Publish(null)
    //
    // Right click (unit selected, not over UI):
    //   → clicked on IDamageable (building/unit) → UnitBase.AttackTarget(cell, target)
    //   → clicked on walkable cell              → UnitBase.MoveTo(cell)
    //
    // Path preview (unit selected, mouse moving over grid):
    //   → A* from unit's cell to hovered cell, drawn by PathPreviewRenderer (1 draw call).
    //
    // Responds to SelectionChangedEvent(null) to keep _currentSelected in sync.
    //
    // IGridProvider is injected via Inject() (DIP). SelectionController performs only
    // read operations on the grid, so IGridProvider is the correct minimal dependency (ISP).
    // GameBootstrapper is the sole caller of Inject() — it is the composition root.
    public class SelectionController : MonoBehaviour
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//
        [Tooltip("When placement mode is active, selection clicks are suppressed.")]
        [SerializeField] private BuildingPlacementController _placementController;

        [Tooltip("Renders the A* path preview cells when a unit is selected.")]
        [SerializeField] private PathPreviewRenderer _pathPreviewRenderer;

        //------Private Variables-------//
        private Camera _camera;
        private ISelectable _currentSelected;
        private IGridProvider _grid;

        // Tracks the last hovered grid cell to avoid recalculating A* every frame.
        private Vector2Int _lastHoverCell = new Vector2Int(int.MinValue, int.MinValue);

        private UnitBase SelectedUnit => _currentSelected as UnitBase;

        #region UNITY_METHODS

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            EventBus<SelectionChangedEvent>.Subscribe(HandleExternalSelectionChange);
        }

        private void OnDisable()
        {
            EventBus<SelectionChangedEvent>.Unsubscribe(HandleExternalSelectionChange);
        }

        private void Update()
        {
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool placing = _placementController != null && _placementController.IsPlacing;

            if (Input.GetMouseButtonDown(0) && !overUI && !placing)
                HandleLeftClick();

            if (Input.GetMouseButtonDown(1) && !overUI && !placing)
                HandleRightClick();

            if (!overUI)
                UpdatePathPreview();
            else
                _pathPreviewRenderer?.Clear();
        }

        #endregion

        #region PUBLIC_METHODS

        // Called by GameBootstrapper to inject the grid dependency.
        // Must be called before the first Update() frame.
        public void Inject(IGridProvider grid)
        {
            _grid = grid;
        }

        #endregion

        #region PRIVATE_METHODS

        // Left click: select building (via grid) or unit (via UnitRegistry).
        private void HandleLeftClick()
        {
            if (_grid == null) { ClearSelection(); return; }

            Vector3    worldPos = GetMouseWorldPosition();
            Vector2Int coord    = _grid.WorldToGrid(worldPos);
            GridCell   cell     = _grid.GetCell(coord);

            // Priority 1: building occupying that grid cell.
            if (cell != null && cell.IsOccupied)
            {
                ISelectable selectable = cell.Occupant.GetComponent<ISelectable>();
                if (selectable != null && selectable != _currentSelected)
                {
                    Select(selectable);
                    return;
                }
                if (selectable == _currentSelected) return;
            }

            // Priority 2: unit registered at that grid cell.
            UnitBase unit = UnitRegistry.GetUnitAt(coord);
            if (unit != null)
            {
                if (unit != _currentSelected)
                    Select(unit);
                return;
            }

            // Empty cell → clear selection.
            ClearSelection();
        }

        // Right click: command selected unit to move or attack.
        private void HandleRightClick()
        {
            UnitBase actor = SelectedUnit;
            if (actor == null) return;

            if (_grid == null) return;

            Vector3    worldPos = GetMouseWorldPosition();
            Vector2Int coord    = _grid.WorldToGrid(worldPos);
            GridCell   cell     = _grid.GetCell(coord);

            if (cell == null) return;

            // Attack building occupying that cell.
            if (cell.IsOccupied)
            {
                IDamageable target = cell.Occupant.GetComponent<IDamageable>();
                if (target != null && !target.IsDead)
                {
                    actor.AttackTarget(coord, target);
                    _pathPreviewRenderer?.Clear();
                    return;
                }
            }

            // Attack unit at that cell (excluding self).
            UnitBase targetUnit = UnitRegistry.GetUnitAt(coord);
            if (targetUnit != null && targetUnit != actor && !targetUnit.IsDead)
            {
                actor.AttackTarget(coord, targetUnit);
                _pathPreviewRenderer?.Clear();
                return;
            }

            // Move to walkable cell.
            if (cell.IsWalkable)
            {
                actor.MoveTo(coord);
                _pathPreviewRenderer?.Clear();
            }
        }

        // Redraws path preview when a unit is selected and the hovered cell changes.
        private void UpdatePathPreview()
        {
            UnitBase actor = SelectedUnit;
            if (actor == null)
            {
                _pathPreviewRenderer?.Clear();
                _lastHoverCell = new Vector2Int(int.MinValue, int.MinValue);
                return;
            }

            if (_grid == null) return;

            Vector3    worldPos  = GetMouseWorldPosition();
            Vector2Int hoverCell = _grid.WorldToGrid(worldPos);

            if (hoverCell == _lastHoverCell) return;
            _lastHoverCell = hoverCell;

            if (!_grid.IsValidCoordinate(hoverCell))
            {
                _pathPreviewRenderer?.Clear();
                return;
            }

            List<Vector2Int> path = AStarPathfinder.FindPath(actor.GridPosition, hoverCell, _grid);

            if (path == null || path.Count == 0)
                _pathPreviewRenderer?.Clear();
            else
                _pathPreviewRenderer?.ShowPath(path, _grid);
        }

        // Selects the given selectable, deselecting the previous one.
        private void Select(ISelectable selectable)
        {
            _currentSelected?.OnDeselected();
            _currentSelected = selectable;
            _currentSelected.OnSelected(); // fires EventBus<SelectionChangedEvent>.Publish internally
            _lastHoverCell = new Vector2Int(int.MinValue, int.MinValue);
        }

        // Clears the current selection and broadcasts null to close the info panel.
        private void ClearSelection()
        {
            if (_currentSelected == null) return;

            _currentSelected.OnDeselected();
            _currentSelected = null;
            _pathPreviewRenderer?.Clear();
            EventBus<SelectionChangedEvent>.Publish(new SelectionChangedEvent(null));
        }

        // Responds to SelectionChangedEvent(null) from external sources (X button, building deleted, etc.)
        // so that highlight is cleaned up and path preview is cleared.
        private void HandleExternalSelectionChange(SelectionChangedEvent e)
        {
            if (e.Selected != null) return;
            if (_currentSelected == null) return;

            _currentSelected.OnDeselected();
            _currentSelected = null;
            _pathPreviewRenderer?.Clear();
            _lastHoverCell = new Vector2Int(int.MinValue, int.MinValue);
        }

        private Vector3 GetMouseWorldPosition()
        {
            return _camera.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                Mathf.Abs(_camera.transform.position.z)));
        }

        #endregion
    }
}
