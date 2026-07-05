using UnityEngine;
using UnityEngine.EventSystems;
using StrategyGame.Core;
using StrategyGame.Data;
using StrategyGame.Grid;

namespace StrategyGame.Buildings
{
    // BuildingPlacementController is responsible for managing the building placement mode.
    // When a building is clicked in the Production menu, the GameEvents.OnBuildingProductionRequested
    // event is triggered, and the placement mode is entered.
    //
    // Placement mode:
    //   - The ghost sprite of the selected building follows the mouse in grid-snapped position.
    //   - GridHighlighter shows a green/red overlay based on the area's validity.
    //   - Left click (if not over UI) + valid area → BuildingFactory is used to place the building.
    //   - Right click or ESC → cancel the placement.
    public class BuildingPlacementController : MonoBehaviour
    {
        //-------Public Variables-------//
        public bool IsPlacing => _isPlacing;

        //------Serialized Fields-------//
        [Header("References")]
        [Tooltip("Overlay for building placement preview on the grid. Should be assigned to the GridHighlighter in the scene.")]
        [SerializeField] private GridHighlighter _highlighter;


        [Header("Ghost Settings")]
        [Tooltip("Transparency level of the ghost sprite (0 = fully transparent, 1 = fully opaque).")]
        [SerializeField, Range(0.1f, 1f)] private float _ghostAlpha = 0.65f;

        [Tooltip("Sorting order of the ghost SpriteRenderer.")]
        [SerializeField] private int _ghostSortingOrder = 5;

        //------Private Variables-------//
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
            GameEvents.OnBuildingProductionRequested += StartPlacement;
        }

        private void OnDisable()
        {
            GameEvents.OnBuildingProductionRequested -= StartPlacement;
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

        // Starts the placement mode with the given BuildingData.
        // If there is an active placement, it is cancelled first.
        public void StartPlacement(BuildingData data)
        {
            if (data == null) return;

            if (_isPlacing)
                ExitPlacementMode();

            _pendingData = data;
            _isPlacing = true;

            CreateGhost(data);
            GameEvents.PlacementModeEntered(data);
        }

        // Cancels the placement mode; ghost and highlight are cleared.
        public void CancelPlacement()
        {
            if (!_isPlacing) return;
            ExitPlacementMode();
        }

        #endregion

        #region PRIVATE_METHODS

        // Updates the ghost and highlighter every frame.
        private void UpdateGhostAndHighlight()
        {
            IGridService grid = GridManager.Instance;
            if (grid == null || _pendingData == null) return;

            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector2Int mouseCell = grid.WorldToGrid(mouseWorld);

            // Align the building to the mouse cell (intentional integer division: single-width buildings align to top-left)
            _currentOrigin = new Vector2Int(
                mouseCell.x - _pendingData.Size.x / 2,
                mouseCell.y - _pendingData.Size.y / 2
            );

            _isCurrentValid = grid.IsAreaFree(_currentOrigin, _pendingData.Size);

            Vector3 areaCenter = grid.GetAreaWorldCenter(_currentOrigin, _pendingData.Size);
            if (_ghostObject != null)
                _ghostObject.transform.position = areaCenter;

            _highlighter?.Show(grid, _currentOrigin, _pendingData.Size, _isCurrentValid);
        }

        // Places the building at the current grid origin and exits the placement mode.
        private void ConfirmPlacement()
        {
            BuildingBase placed = BuildingFactory.Create(_pendingData, _currentOrigin);

            // Factory returns null in invalid areas; this is rare but possible when _isCurrentValid is true.
            if (placed != null)
                ExitPlacementMode();
        }

        // Clears all placement state and triggers the PlacementModeExited event.
        private void ExitPlacementMode()
        {
            _isPlacing = false;
            _pendingData = null;

            DestroyGhost();
            _highlighter?.Hide();

            GameEvents.PlacementModeExited();
        }

        // Creates the building ghost: tries to use the prefab's sprite, falls back to the icon.
        private void CreateGhost(BuildingData data)
        {
            _ghostObject = new GameObject("BuildingGhost");

            _ghostRenderer = _ghostObject.AddComponent<SpriteRenderer>();
            _ghostRenderer.sprite = ResolveGhostSprite(data);
            _ghostRenderer.color = new Color(1f, 1f, 1f, _ghostAlpha);
            _ghostRenderer.sortingOrder = _ghostSortingOrder;

            ScaleGhostToBuilding(data);
        }

        // Scales the ghost to the target building's grid footprint (size * cellSize).
        private void ScaleGhostToBuilding(BuildingData data)
        {
            IGridService grid = GridManager.Instance;
            if (grid == null || _ghostRenderer == null || _ghostRenderer.sprite == null) return;

            float targetWidth = data.Size.x * grid.CellSize;
            float targetHeight = data.Size.y * grid.CellSize;

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

            Destroy(_ghostObject);
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
            // For orthographic camera, z = distance of the camera to the grid plane
            screen.z = Mathf.Abs(_camera.transform.position.z);
            return _camera.ScreenToWorldPoint(screen);
        }

        // Checks if the mouse is over the UI; prevents UI clicks from registering on the grid.
        private static bool IsPointerOverUI()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        #endregion
    }
}
