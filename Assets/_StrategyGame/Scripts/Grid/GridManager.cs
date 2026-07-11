using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame.Grid
{
    public class GridManager : MonoBehaviour, IGridService
    {
        //-------Public Variables-------//
        public event Action<GridAreaChangedArgs> OnAreaOccupancyChanged;
        public static IGridService Instance { get; private set; }
        public int Width => _config.Width;
        public int Height => _config.Height;
        public float CellSize => _config.CellSize;

        //------Serialized Fields-------//
        [SerializeField] private GridConfig _config;

        //------Private Variables-------//
        private GridCell[,] _cells;

        #region UNITY_METHODS

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeGrid();
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
                Instance = null;
        }

        #endregion

        #region PUBLIC_METHODS

        public GridCell GetCell(int x, int y)
                   => IsValidCoordinate(x, y) ? _cells[x, y] : null;

        public GridCell GetCell(Vector2Int coordinate)
            => GetCell(coordinate.x, coordinate.y);

        public bool IsValidCoordinate(int x, int y)
            => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsValidCoordinate(Vector2Int coordinate)
            => IsValidCoordinate(coordinate.x, coordinate.y);

        // Returns the world-space centre of the given grid cell.
        // Formula: origin + (coord + 0.5) * cellSize
        public Vector3 GridToWorld(Vector2Int coordinate)
            => _config.Origin + new Vector3(
                (coordinate.x + 0.5f) * CellSize,
                (coordinate.y + 0.5f) * CellSize,
                0f);

        // Returns the grid coordinate containing the given world position.
        // Inverse of GridToWorld
        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 relative = worldPosition - _config.Origin;
            return new Vector2Int(
                Mathf.FloorToInt(relative.x / CellSize),
                Mathf.FloorToInt(relative.y / CellSize));
        }

        public IEnumerable<GridCell> GetNeighbors(GridCell cell, bool includeDiagonals = false)
        {
            int cx = cell.Coordinate.x;
            int cy = cell.Coordinate.y;
            GridCell n;

            if ((n = GetCell(cx + 1, cy)) != null) yield return n;
            if ((n = GetCell(cx - 1, cy)) != null) yield return n;
            if ((n = GetCell(cx, cy + 1)) != null) yield return n;
            if ((n = GetCell(cx, cy - 1)) != null) yield return n;

            if (!includeDiagonals) yield break;

            if ((n = GetCell(cx + 1, cy + 1)) != null) yield return n;
            if ((n = GetCell(cx - 1, cy + 1)) != null) yield return n;
            if ((n = GetCell(cx + 1, cy - 1)) != null) yield return n;
            if ((n = GetCell(cx - 1, cy - 1)) != null) yield return n;
        }

        public bool IsAreaFree(Vector2Int origin, Vector2Int size)
        {
            for (int x = origin.x; x < origin.x + size.x; x++)
                for (int y = origin.y; y < origin.y + size.y; y++)
                {
                    if (!IsValidCoordinate(x, y)) return false;
                    if (!_cells[x, y].IsWalkable) return false;
                }
            return true;
        }

        // Returns the world-space centre of a rectangular grid area.
        // GridToWorld(origin) gives the centre of the bottom-left cell;
        // offset by (size - 1) * 0.5 * cellSize to reach the full area's centre.
        public Vector3 GetAreaWorldCenter(Vector2Int origin, Vector2Int size)
        {
            Vector3 cellCentre = GridToWorld(origin);
            return cellCentre + new Vector3(
                (size.x - 1) * 0.5f * CellSize,
                (size.y - 1) * 0.5f * CellSize,
                0f);
        }

        public bool TryOccupyArea(Vector2Int origin, Vector2Int size, GameObject occupant)
        {
            if (!IsAreaFree(origin, size)) return false;

            ApplyToArea(origin, size, cell => cell.SetOccupant(occupant));
            OnAreaOccupancyChanged?.Invoke(new GridAreaChangedArgs(origin, size, isOccupied: true));
            return true;
        }

        public void FreeArea(Vector2Int origin, Vector2Int size)
        {
            ApplyToArea(origin, size, cell => cell.SetOccupant(null));
            OnAreaOccupancyChanged?.Invoke(new GridAreaChangedArgs(origin, size, isOccupied: false));
        }

        #endregion

        #region PRIVATE_METHODS

        private void InitializeGrid()
        {
            _cells = new GridCell[Width, Height];

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var coord = new Vector2Int(x, y);
                    _cells[x, y] = new GridCell(coord, GridToWorld(coord));
                }
        }

        private void ApplyToArea(Vector2Int origin, Vector2Int size, Action<GridCell> action)
        {
            for (int x = origin.x; x < origin.x + size.x; x++)
                for (int y = origin.y; y < origin.y + size.y; y++)
                    if (IsValidCoordinate(x, y))
                        action(_cells[x, y]);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_config == null) return;

            float cs = _config.CellSize;

            for (int x = 0; x < _config.Width; x++)
                for (int y = 0; y < _config.Height; y++)
                {
                    bool walkable = _cells == null || _cells[x, y].IsWalkable;
                    Gizmos.color = walkable
                        ? new Color(0.3f, 0.8f, 0.3f, 0.15f)
                        : new Color(1f, 0.2f, 0.2f, 0.35f);

                    Vector3 center = _config.Origin + new Vector3((x + 0.5f) * cs, (y + 0.5f) * cs, 0f);
                    Gizmos.DrawWireCube(center, new Vector3(cs, cs, 0f) * 0.97f);
                }
        }
#endif
        #endregion
    }
}
