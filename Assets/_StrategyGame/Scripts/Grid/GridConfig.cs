using UnityEngine;

namespace StrategyGame.Grid
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "StrategyGame/Grid/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        //-------Public Variables-------//
        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public Vector3 Origin => _origin;

        //------Serialized Fields-------//
        [Header("Dimensions")]
        [SerializeField, Min(1)] private int _width = 20;
        [SerializeField, Min(1)] private int _height = 15;

        [Header("Cell")]
        [Tooltip("Edge length of one square cell in Unity world units.")]
        [SerializeField, Min(0.1f)] private float _cellSize = 1f;

        [Header("Origin")]
        [Tooltip("World-space position of the bottom-left corner of cell (0, 0).")]
        [SerializeField] private Vector3 _origin = Vector3.zero;

        //------Private Variables-------//

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        #endregion
    }
}
