using System.Collections;
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

        [Header("Damage Flash")]
        [Tooltip("Color applied to the sprite briefly when damage is taken.")]
        [SerializeField] private Color _damageFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
        [Tooltip("Duration in seconds of the damage flash.")]
        [SerializeField] private float _damageFlashDuration = 0.12f;

        //------Private Variables-------//
        private Color _defaultTint;
        private Coroutine _flashCoroutine;

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

        // Briefly flashes the sprite red to communicate that damage was received.
        // Captures the current color before the flash so it correctly restores
        // both the default and the selected-highlight state without needing to
        // track selection state here.
        public void FlashDamage()
        {
            if (_spriteRenderer == null) return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);

            _flashCoroutine = StartCoroutine(DamageFlashCoroutine());
        }

        #endregion

        #region PRIVATE_METHODS

        private IEnumerator DamageFlashCoroutine()
        {
            Color restoreColor = _spriteRenderer.color;
            _spriteRenderer.color = _damageFlashColor;
            yield return new WaitForSeconds(_damageFlashDuration);
            _spriteRenderer.color = restoreColor;
            _flashCoroutine = null;
        }

        #endregion
    }
}
