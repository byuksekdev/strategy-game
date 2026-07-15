using Lean.Pool;
using UnityEngine;
using UnityEngine.EventSystems;
using StrategyGame.Core;
using StrategyGame.Data;
using StrategyGame.Grid;

namespace StrategyGame.Buildings
{
    // Manages building placement mode.
    // When a building card is clicked in the Production menu, BuildingProductionRequestedEvent
    // is published on the EventBus. This controller subscribes to that event and enters
    // placement mode.
    //
    // Placement mode:
    //   - The ghost sprite of the selected building follows the mouse in grid-snapped position.
    //   - GridHighlighter shows a green/red overlay based on the area's validity.
    //   - Left click (if not over UI) + valid area → IBuildingFactory places the building.
    //   - Right click or ESC → cancel the placement.
    //
    // Dependencies are injected via Inject() (DIP / method injection).
    // BuildingPlacementController only reads the grid (preview, ghost scaling),
    // so IGridProvider is the correct minimal dependency (ISP).
    // GameBootstrapper is the sole caller of Inject() — it is the composition root.
    public class BuildingPlacementController : MonoBehaviour
    {
        //-------Public Variables-------//
        public bool IsPlacing => _isPlacing;

        //------Serialized Fields-------//
        [Header("References")]
        [Tooltip("Overlay for building placement preview on the grid.")]
        [SerializeField] private GridHighlighter _highlighter;

        [Header("Ghost Settings")]
        [Tooltip("Prefab with a SpriteRenderer used as the placement ghost. Pooled via LeanPool.")]
        [SerializeField] private GameObject _ghostPrefab;

        [Tooltip("Transparency level of the ghost sprite (0 = fully transparent, 1 = fully opaque).")]
        [SerializeField, Range(0.1f, 1f)] private float _ghostAlpha = 0.65f;

        [Tooltip("Sorting order of the ghost SpriteRenderer.")]
        [SerializeField] private int _ghostSortingOrder = 5;

        //------Private Variables-------//
        private IBuildingFactory _buildingFactory;
        private IGridProvider _grid;
        private Camera _camera;
        private bool _isPlacing;
        private BuildingData _pendingData;
        private GameObject _ghostObject;
        private SpriteRenderer _ghostRenderer;
        private Vector2Int _currentOrigin;
        private bool _isCurrentValid;

        #region UNITY_METHODS

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BuildingProductionRequestedEvent>(HandleBuildingProductionRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BuildingProductionRequestedEvent>(HandleBuildingProductionRequested);
        }

        private void OnDestroy()
        {
            DestroyGhost();
        }

        private void Update()
        {
            if (!_isPlacing) return;

            UpdateGhostAndHighlight();

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                if (_isCurrentValid)
                    ConfirmPlacement();
            }
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }

        #endregion

        #region PUBLIC_METHODS

        // Called by GameBootstrapper to inject the factory implementation.
        // Also accepts test doubles for unit tests.
        public void Inject(IBuildingFactory buildingFactory)
        {
            _buildingFactory = buildingFactory;
        }

        // Called by GameBootstrapper to inject the grid read dependency.
        public void Inject(IGridProvider grid)
        {
            _grid = grid;
        }

        // Cancels the placement mode; ghost and highlight are cleared.
        public void CancelPlacement()
        {
            if (!_isPlacing) return;
            ExitPlacementMode();
        }

        #endregion

        #region PRIVATE_METHODS

        private void HandleBuildingProductionRequested(BuildingProductionRequestedEvent e)
        {
            StartPlacement(e.BuildingData);
        }

        // Starts the placement mode with the given BuildingData.
        // If there is an active placement, it is cancelled first.
        private void StartPlacement(BuildingData data)
        {
            if (data == null) return;

            if (_isPlacing)
                ExitPlacementMode();

            _pendingData = data;
            _isPlacing = true;

            CreateGhost(data);
        }

        // Updates the ghost and highlighter every frame.
        private void UpdateGhostAndHighlight()
        {
            if (_grid == null || _pendingData == null) return;

            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector2Int mouseCell = _grid.WorldToGrid(mouseWorld);

            // Align the building to the mouse cell (intentional integer division: single-width buildings align to top-left)
            _currentOrigin = new Vector2Int(
                mouseCell.x - _pendingData.Size.x / 2,
                mouseCell.y - _pendingData.Size.y / 2
            );

            _isCurrentValid = _grid.IsAreaFree(_currentOrigin, _pendingData.Size);

            Vector3 areaCenter = _grid.GetAreaWorldCenter(_currentOrigin, _pendingData.Size);
            if (_ghostObject != null)
                _ghostObject.transform.position = areaCenter;

            _highlighter?.Show(_grid, _currentOrigin, _pendingData.Size, _isCurrentValid);
        }

        // Places the building at the current grid origin and exits the placement mode.
        private void ConfirmPlacement()
        {
            BuildingBase placed = _buildingFactory.Create(_pendingData, _currentOrigin);

            // Factory returns null in invalid areas; this is rare but possible when _isCurrentValid is true.
            if (placed != null)
                ExitPlacementMode();
        }

        // Clears all placement state and publishes PlacementModeExitedEvent.
        private void ExitPlacementMode()
        {
            _isPlacing = false;
            _pendingData = null;

            DestroyGhost();
            _highlighter?.Hide();

            EventBus.Publish(new PlacementModeExitedEvent());
        }

        // Creates the building ghost: spawns _ghostPrefab via LeanPool, tries to use the
        // prefab's sprite, falls back to the icon.
        private void CreateGhost(BuildingData data)
        {
            _ghostObject = LeanPool.Spawn(_ghostPrefab);
            _ghostObject.name = "BuildingGhost";

            _ghostRenderer = _ghostObject.GetComponent<SpriteRenderer>();
            _ghostRenderer.sprite = ResolveGhostSprite(data);
            _ghostRenderer.color = new Color(1f, 1f, 1f, _ghostAlpha);
            _ghostRenderer.sortingOrder = _ghostSortingOrder;

            ScaleGhostToBuilding(data);
        }

        // Scales the ghost to the target building's grid footprint (size * cellSize).
        private void ScaleGhostToBuilding(BuildingData data)
        {
            if (_grid == null || _ghostRenderer == null || _ghostRenderer.sprite == null) return;

            float targetWidth = data.Size.x * _grid.CellSize;
            float targetHeight = data.Size.y * _grid.CellSize;

            float spriteWidth = _ghostRenderer.sprite.rect.width / _ghostRenderer.sprite.pixelsPerUnit;
            float spriteHeight = _ghostRenderer.sprite.rect.height / _ghostRenderer.sprite.pixelsPerUnit;

            if (spriteWidth <= 0f || spriteHeight <= 0f) return;

            _ghostObject.transform.localScale = new Vector3(
                targetWidth / spriteWidth,
                targetHeight / spriteHeight,
                1f
            );
        }

        private void DestroyGhost()
        {
            if (_ghostObject == null) return;

            LeanPool.Despawn(_ghostObject);
            _ghostObject = null;
            _ghostRenderer = null;
        }

        // Prefab's SpriteRenderer sprite is preferred; if not, falls back to EntityData.Icon.
        private static Sprite ResolveGhostSprite(BuildingData data)
        {
            if (data.Prefab != null)
            {
                SpriteRenderer sr = data.Prefab.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) return sr.sprite;
            }
            return data.Icon;
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 screen = Input.mousePosition;
            screen.z = Mathf.Abs(_camera.transform.position.z);
            return _camera.ScreenToWorldPoint(screen);
        }

        // Checks if the mouse is over the UI; prevents UI clicks from registering on the grid.
        private static bool IsPointerOverUI()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        #endregion
    }
}
