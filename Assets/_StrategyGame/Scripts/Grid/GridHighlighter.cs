using UnityEngine;

namespace StrategyGame.Grid
{
    // Renders a semi-transparent quad over a rectangular grid area to preview
    // building placement before it is confirmed.
    // Green = all cells are free and the placement is valid.
    // Red   = one or more cells are blocked or out of bounds.
    // Uses MaterialPropertyBlock for colour changes — always 1 draw call while visible.
    // Attach to a dedicated child GameObject (e.g. Grid/Highlighter).
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridHighlighter : MonoBehaviour
    {
        //------Serialized Fields-------//
        [Header("Material")]
        [Tooltip("Unlit transparent material (Alpha Blend). _Color is driven at runtime via MaterialPropertyBlock.")]
        [SerializeField] private Material _overlayMaterial;

        [Header("Colors")]
        [SerializeField] private Color _validColor = new Color(0.00f, 1.00f, 0.00f, 0.30f);
        [SerializeField] private Color _invalidColor = new Color(1.00f, 0.10f, 0.10f, 0.35f);

        [Header("Sorting")]
        [SerializeField] private int _sortingOrder = 10;

        //------Private Variables-------//
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private Mesh _quadMesh;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // -----------------------------------------------------------------------

        #region UNITY_METHODS

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();

            _quadMesh = BuildUnitQuad();
            _meshFilter.sharedMesh = _quadMesh;
            _meshRenderer.sharedMaterial = _overlayMaterial;
            _meshRenderer.sortingOrder = _sortingOrder;
            _meshRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_quadMesh != null) Destroy(_quadMesh);
        }

        #endregion

        #region PUBLIC_METHODS

        // Positions the overlay at the given grid area and colours it by placement validity.
        // origin : bottom-left cell coordinate of the area
        // size   : width and height in grid cells
        // isValid: green when true, red when false
        public void Show(IGridProvider grid, Vector2Int origin, Vector2Int size, bool isValid)
        {
            if (grid == null) return;

            // GridToWorld(origin) returns the centre of the bottom-left cell.
            // Shift by (size - 1) * 0.5 * cellSize to reach the full area's centre.
            Vector3 cellCentre = grid.GridToWorld(origin);
            Vector3 areaCenter = cellCentre + new Vector3(
                (size.x - 1) * 0.5f * grid.CellSize,
                (size.y - 1) * 0.5f * grid.CellSize,
                0f);

            transform.position = areaCenter;
            transform.localScale = new Vector3(
                size.x * grid.CellSize,
                size.y * grid.CellSize,
                1f);

            _propertyBlock.SetColor(ColorId, isValid ? _validColor : _invalidColor);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
            _meshRenderer.enabled = true;
        }

        // Hides the overlay. Call when placement mode ends.
        public void Hide() => _meshRenderer.enabled = false;

        #endregion

        #region PRIVATE_METHODS

        // Creates a unit quad centred at the origin (−0.5 … 0.5 on each axis).
        // Scale is applied via Transform.localScale at runtime inside Show().
        private static Mesh BuildUnitQuad()
        {
            var mesh = new Mesh { name = "HighlightQuad" };

            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), // bottom-left
                new Vector3( 0.5f, -0.5f, 0f), // bottom-right
                new Vector3( 0.5f,  0.5f, 0f), // top-right
                new Vector3(-0.5f,  0.5f, 0f), // top-left
            });

            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);

            mesh.SetUVs(0, new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            });

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        #endregion
    }
}
