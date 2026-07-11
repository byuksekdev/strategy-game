using UnityEngine;

namespace StrategyGame.Buildings
{
    // Owns all visual highlight logic for a selected building.
    // Attach to the same GameObject as BuildingBase.
    // BuildingBase calls Highlight() / ClearHighlight() — it has no knowledge of how the
    // visual is achieved, keeping rendering concerns out of the game-logic layer.
    public class SelectionHighlight : MonoBehaviour
    {
        //------Serialized Fields-------//
        [Tooltip("The SpriteRenderer used for the selection visual (e.g. an outline child sprite).")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Tooltip("Tint applied to the sprite while this building is selected.")]
        [SerializeField] private Color _selectedTint = new Color(1f, 0.85f, 0.25f, 1f);

        //------Private Variables-------//
        private Color _defaultTint;

        #region UNITY_METHODS

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _defaultTint = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
        }

        #endregion

        #region PUBLIC_METHODS

        public void Highlight()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.color = _selectedTint;
        }

        public void ClearHighlight()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.color = _defaultTint;
        }

        #endregion
    }
}
