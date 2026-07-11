using System.Collections.Generic;
using UnityEngine;
using StrategyGame.Grid;

namespace StrategyGame.Grid
{
    // Renders the A* path preview as a single procedural mesh — always 1 draw call.
    // Each cell in the path is represented by a full-coverage quad within its grid tile.
    //
    // Requires a semi-transparent Unlit material (Alpha Blend, _Color driven at runtime).
    // Suggested sorting order: above grid lines (−1) but below buildings/units.
    //
    // Usage:
    //   ShowPath(path, grid) — rebuilds mesh and makes renderer visible.
    //   Clear()              — hides renderer; does not destroy the mesh.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PathPreviewRenderer : MonoBehaviour
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//
        [Header("Material")]
        [Tooltip("Unlit transparent material. _Color is driven at runtime via MaterialPropertyBlock.")]
        [SerializeField] private Material _material;

        [Header("Appearance")]
        [SerializeField] private Color _pathColor = new Color(0.20f, 0.65f, 1.00f, 0.35f);
        [SerializeField] private int _sortingOrder = 3;

        //------Private Variables-------//
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private Mesh _mesh;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        #region UNITY_METHODS

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();

            _mesh = new Mesh { name = "PathPreviewMesh" };
            _meshFilter.sharedMesh = _mesh;

            if (_material != null)
                _meshRenderer.sharedMaterial = _material;

            _meshRenderer.sortingOrder = _sortingOrder;
            _meshRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }

        #endregion

        #region PUBLIC_METHODS

        // Rebuilds the mesh to cover every cell in the path and shows the renderer.
        // path  : ordered list of grid coordinates (start-exclusive, end-inclusive).
        // grid  : used to convert coordinates to world positions.
        public void ShowPath(List<Vector2Int> path, IGridProvider grid)
        {
            if (path == null || path.Count == 0 || grid == null)
            {
                Clear();
                return;
            }

            int count = path.Count;
            float halfCell = grid.CellSize * 0.5f;

            var vertices = new Vector3[count * 4];
            var triangles = new int[count * 6];
            var uvs = new Vector2[count * 4];

            for (int i = 0; i < count; i++)
            {
                Vector3 centre = grid.GridToWorld(path[i]);

                int vi = i * 4;
                vertices[vi] = centre + new Vector3(-halfCell, -halfCell, 0f);
                vertices[vi + 1] = centre + new Vector3(halfCell, -halfCell, 0f);
                vertices[vi + 2] = centre + new Vector3(halfCell, halfCell, 0f);
                vertices[vi + 3] = centre + new Vector3(-halfCell, halfCell, 0f);

                int ti = i * 6;
                triangles[ti] = vi; triangles[ti + 1] = vi + 2; triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi; triangles[ti + 4] = vi + 3; triangles[ti + 5] = vi + 2;

                uvs[vi] = new Vector2(0f, 0f);
                uvs[vi + 1] = new Vector2(1f, 0f);
                uvs[vi + 2] = new Vector2(1f, 1f);
                uvs[vi + 3] = new Vector2(0f, 1f);
            }

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(triangles, 0);
            _mesh.SetUVs(0, uvs);
            _mesh.RecalculateBounds();

            _propertyBlock.SetColor(ColorId, _pathColor);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
            _meshRenderer.enabled = true;
        }

        // Hides the path overlay without destroying the mesh.
        public void Clear()
        {
            if (_meshRenderer != null)
                _meshRenderer.enabled = false;
        }

        #endregion

        #region PRIVATE_METHODS

        #endregion
    }
}
