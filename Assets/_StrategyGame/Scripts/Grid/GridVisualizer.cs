using UnityEngine;

namespace StrategyGame.Grid
{
    // Renders the entire grid as a single procedural mesh — 1 draw call regardless of grid size.
    // Each line is built as a quad (2 triangles) so that line thickness is controllable.
    // Requires an Unlit / vertex-colour material (e.g. URP Unlit with Vertex Color enabled).
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridVisualizer : MonoBehaviour
    {
        //------Serialized Fields-------//
        [Header("Material")]
        [Tooltip("Unlit vertex-colour material for the grid lines.")]
        [SerializeField] private Material _lineMaterial;

        [Header("Appearance")]
        [SerializeField] private Color _lineColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField, Range(0.005f, 0.2f)]
        private float _lineThickness = 0.04f;
        [SerializeField] private int _sortingOrder = -1;

        //------Private Variables-------//
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private IGridProvider _grid;

        // -----------------------------------------------------------------------

        #region UNITY_METHODS

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }

        #endregion

        #region PUBLIC_METHODS

        // Called by GameBootstrapper to provide the grid abstraction (DIP).
        public void Inject(IGridProvider grid)
        {
            _grid = grid;
            BuildMesh(_grid);
        }

        // Call this to regenerate the mesh at runtime (e.g. after a grid resize).
        public void Rebuild()
        {
            if (_mesh != null) Destroy(_mesh);

            if (_grid != null)
                BuildMesh(_grid);
        }

        #endregion

        #region PRIVATE_METHODS

        // Builds the grid mesh from grid parameters.
        // Each line segment is a quad (4 verts, 2 tris) for thickness support.
        // After GPU upload the CPU copy is freed via UploadMeshData(true).
        private void BuildMesh(IGridProvider grid)
        {
            int cols = grid.Width;
            int rows = grid.Height;
            float cellSize = grid.CellSize;
            float halfThick = _lineThickness * 0.5f;

            // (cols+1) vertical lines + (rows+1) horizontal lines
            int lineCount = (cols + 1) + (rows + 1);

            var vertices = new Vector3[lineCount * 4]; // 4 corners per quad
            var colors = new Color[lineCount * 4];
            var triangles = new int[lineCount * 6];     // 2 triangles × 3 indices per quad

            // Bottom-left world corner of cell (0,0)
            Vector3 origin = grid.GridToWorld(Vector2Int.zero)
                             - new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0f);

            int vi = 0; // vertex cursor
            int ti = 0; // triangle cursor

            // Vertical lines  (x = 0 … cols)
            for (int x = 0; x <= cols; x++)
            {
                Vector3 min = origin + new Vector3(x * cellSize - halfThick, 0f, 0f);
                Vector3 max = origin + new Vector3(x * cellSize + halfThick, rows * cellSize, 0f);
                AddQuad(min, max, ref vi, ref ti, vertices, colors, triangles);
            }

            // Horizontal lines  (y = 0 … rows)
            for (int y = 0; y <= rows; y++)
            {
                Vector3 min = origin + new Vector3(0f, y * cellSize - halfThick, 0f);
                Vector3 max = origin + new Vector3(cols * cellSize, y * cellSize + halfThick, 0f);
                AddQuad(min, max, ref vi, ref ti, vertices, colors, triangles);
            }

            _mesh = new Mesh { name = "GridLineMesh" };
            _mesh.SetVertices(vertices);
            _mesh.SetColors(colors);
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateBounds();
            _mesh.UploadMeshData(true); // free CPU copy after GPU upload

            _meshFilter.sharedMesh = _mesh;
            _meshRenderer.sharedMaterial = _lineMaterial;
            _meshRenderer.sortingOrder = _sortingOrder;
        }

        // Writes 4 vertices and 6 triangle indices for a single axis-aligned quad.
        //   min = bottom-left corner,  max = top-right corner
        private void AddQuad(
            Vector3 min,
            Vector3 max,
            ref int vi,
            ref int ti,
            Vector3[] vertices,
            Color[] colors,
            int[] triangles)
        {
            int baseIdx = vi;

            vertices[vi] = new Vector3(min.x, min.y, 0f); // bottom-left
            vertices[vi + 1] = new Vector3(max.x, min.y, 0f); // bottom-right
            vertices[vi + 2] = new Vector3(max.x, max.y, 0f); // top-right
            vertices[vi + 3] = new Vector3(min.x, max.y, 0f); // top-left

            colors[vi] = colors[vi + 1] = colors[vi + 2] = colors[vi + 3] = _lineColor;

            // Triangle 1: BL → TR → BR  (clockwise)
            triangles[ti] = baseIdx;
            triangles[ti + 1] = baseIdx + 2;
            triangles[ti + 2] = baseIdx + 1;

            // Triangle 2: BL → TL → TR  (clockwise)
            triangles[ti + 3] = baseIdx;
            triangles[ti + 4] = baseIdx + 3;
            triangles[ti + 5] = baseIdx + 2;

            vi += 4;
            ti += 6;
        }

        #endregion
    }
}
