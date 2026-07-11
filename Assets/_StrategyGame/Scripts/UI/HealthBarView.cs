using UnityEngine;
using StrategyGame.Core;

namespace StrategyGame.UI
{
    // World-space health bar rendered via SpriteRenderer for batching compatibility.
    // Attach to a child GameObject of any IDamageable (building or unit).
    //
    // Prefab setup:
    //   HealthBar (this component)
    //     ├── Background  (SpriteRenderer)
    //     └── Fill        (SpriteRenderer) ← any pivot works; position/scale adjusted in code
    //
    // Subscribes to the owner's local OnHealthChanged event rather than the global EventBus.
    // This avoids broadcasting every hit across the entire scene: only this health bar reacts.
    public class HealthBarView : MonoBehaviour
    {
        [Tooltip("Fill sprite (any pivot works — no Sprite Editor change needed).")]
        [SerializeField] private SpriteRenderer _fillRenderer;

        [Tooltip("Background sprite (optional visual).")]
        [SerializeField] private SpriteRenderer _backgroundRenderer;

        private IDamageable _owner;

        // Cached so we can always compute relative to the "full" state.
        private Vector3 _fillOriginalLocalPos;
        private float   _fillOriginalScaleX;

        #region UNITY_METHODS

        private void Awake()
        {
            _owner = GetComponentInParent<IDamageable>();

            if (_fillRenderer != null)
            {
                _fillOriginalLocalPos = _fillRenderer.transform.localPosition;
                _fillOriginalScaleX   = _fillRenderer.transform.localScale.x;
            }
        }

        // OnEnable/OnDisable are used (not Awake/OnDestroy) so pooled objects
        // automatically resubscribe when reactivated and release the reference when returned to pool.
        private void OnEnable()
        {
            if (_owner != null)
                _owner.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (_owner != null)
                _owner.OnHealthChanged -= HandleHealthChanged;
        }

        #endregion

        #region PRIVATE_METHODS

        private void HandleHealthChanged(int currentHP, int maxHP)
        {
            float ratio = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;
            ApplyFill(ratio);
        }

        private void ApplyFill(float ratio)
        {
            if (_fillRenderer == null) return;

            // Scale the fill width down.
            Vector3 scale = _fillRenderer.transform.localScale;
            scale.x = _fillOriginalScaleX * ratio;
            _fillRenderer.transform.localScale = scale;

            // Shift the fill left so its left edge stays fixed, compensating for center pivot.
            // offset = half of the width that was removed.
            Vector3 pos = _fillOriginalLocalPos;
            pos.x -= _fillOriginalScaleX * (1f - ratio) * 0.5f;
            _fillRenderer.transform.localPosition = pos;
        }

        #endregion
    }
}
