using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;

namespace StrategyGame.Buildings
{
    // Barracks: the only building that can produce soldier units.
    // Has a configurable spawn cell offset (relative to GridOrigin) that defines
    // the preferred spawn tile. UnitSpawnSystem performs BFS from this point
    // to find the nearest free cell when units stack up.
    public class Barracks : BuildingBase, IUnitProducer
    {
        //-------Public Variables-------//
        public UnitData[] ProducibleUnits => ProductionData != null
            ? ProductionData.ProducibleUnits
            : System.Array.Empty<UnitData>();

        public bool CanProduceUnits => !IsDead && ProducibleUnits.Length > 0;

        // World-space position of the preferred spawn point.
        // Exposed via IUnitProducer so UnitSpawnSystem can start its BFS here.
        public Vector3 SpawnWorldPosition => _spawnPoint != null
            ? _spawnPoint.position
            : transform.position;

        //------Serialized Fields-------//
        // Offset (in grid cells) from GridOrigin to the preferred spawn cell.
        // Default (1, -1) = 2nd column, one row below the building's bottom edge,
        // which lands at the bottom-middle area of a 4×4 Barracks.
        // Adjust freely in the Inspector to reposition the spawn tile.
        [Tooltip("Grid-cell offset from GridOrigin to the preferred spawn tile. " +
                 "For a 4×4 Barracks the bottom-middle cells are at offsets (1,-1) and (2,-1).")]
        [SerializeField] private Vector2Int _spawnCellOffset = new Vector2Int(1, -1);

        [Tooltip("Custom sprite for the spawn point indicator. Leave empty to use a generated circle.")]
        [SerializeField] private Sprite _spawnIndicatorSprite;

        //------Private Variables-------//
        private Transform _spawnPoint;

        // Cached procedural sprite shared across all Barracks instances.
        private static Sprite _defaultSpawnSprite;

        // Cast helper: safe null if BuildingData was not set as ProductionBuildingData.
        private ProductionBuildingData ProductionData => BuildingData as ProductionBuildingData;

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        protected override void OnInitialized()
        {
            ResolveSpawnPoint();
        }

        // Creates the SpawnPoint Transform and positions it at the configured spawn cell.
        // The desired cell is GridOrigin + _spawnCellOffset.
        // If the coordinate is out of bounds, the spawn point falls back to the building centre.
        private void ResolveSpawnPoint()
        {
            if (_spawnPoint == null)
            {
                _spawnPoint = new GameObject("SpawnPoint").transform;
                _spawnPoint.SetParent(transform);
            }

            if (GridProvider == null)
            {
                _spawnPoint.localPosition = Vector3.zero;
                return;
            }

            Vector2Int desiredCell = GridOrigin + _spawnCellOffset;

            if (GridProvider.IsValidCoordinate(desiredCell))
                _spawnPoint.position = GridProvider.GridToWorld(desiredCell);
            else
                _spawnPoint.localPosition = Vector3.zero;

            AttachSpawnIndicator();
        }

        // Adds a SpriteRenderer to the spawn point GameObject so the player can see
        // exactly where newly produced soldiers will emerge. Uses a custom sprite if
        // assigned in the Inspector, otherwise generates a simple ring at runtime.
        private void AttachSpawnIndicator()
        {
            SpriteRenderer sr = _spawnPoint.gameObject.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = _spawnPoint.gameObject.AddComponent<SpriteRenderer>();

            sr.sprite       = _spawnIndicatorSprite != null ? _spawnIndicatorSprite : GetOrCreateDefaultSpawnSprite();
            sr.color        = new Color(0.2f, 1f, 0.4f, 0.75f);
            sr.sortingOrder = 5;

            _spawnPoint.localScale = Vector3.one * 0.5f;
        }

        // Lazily creates a procedural ring sprite shared across all Barracks instances.
        // The sprite is a static field so it is generated only once per session.
        private static Sprite GetOrCreateDefaultSpawnSprite()
        {
            if (_defaultSpawnSprite != null) return _defaultSpawnSprite;

            const int size   = 64;
            const float center = size * 0.5f;
            const float outerR = center - 1f;
            const float innerR = outerR - 6f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "SpawnIndicator" };

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float d    = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    bool onRing = d >= innerR && d <= outerR;
                    tex.SetPixel(x, y, onRing ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            _defaultSpawnSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _defaultSpawnSprite;
        }

        #endregion
    }
}
