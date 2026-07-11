using System;
using Lean.Pool;
using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;
using StrategyGame.Grid;

namespace StrategyGame.Buildings
{
    // Abstract base for all buildings (Barracks, PowerPlant, etc.).
    // Handles HP, selection callbacks, and grid cleanup on destruction.
    // Concrete buildings inherit from this and override OnInitialized() for custom setup.
    public abstract class BuildingBase : MonoBehaviour, IDamageable, ISelectable, IProducible
    {
        //-------Public Variables-------//
        public int MaxHP => _buildingData != null ? _buildingData.MaxHP : 0;
        public int CurrentHP => _currentHP;
        public bool IsDead => _currentHP <= 0;
        public EntityData Data => _buildingData;
        public BuildingData BuildingData => _buildingData;
        public Vector2Int GridOrigin => _gridOrigin;

        // Fired whenever HP changes. HealthBarView and InformationPanelController
        // subscribe directly instead of listening to a global event bus.
        public event Action<int, int> OnHealthChanged;

        //------Serialized Fields-------//

        //------Private Variables-------//
        private BuildingData _buildingData;
        private Vector2Int _gridOrigin;
        private int _currentHP;

        private SelectionHighlight _highlight;

        #region UNITY_METHODS

        private void Awake()
        {
            _highlight = GetComponent<SelectionHighlight>();
        }

        #endregion

        #region PUBLIC_METHODS

        // Called by BuildingFactory after instantiation to inject runtime data and grid position.
        public void Initialize(BuildingData data, Vector2Int gridOrigin)
        {
            _buildingData = data;
            _gridOrigin = gridOrigin;
            _currentHP = data.MaxHP;
            OnHealthChanged?.Invoke(_currentHP, MaxHP);
            OnInitialized();
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            _currentHP = Mathf.Max(0, _currentHP - amount);
            OnHealthChanged?.Invoke(_currentHP, MaxHP);

            if (IsDead) Die();
        }

        public virtual void Die()
        {
            if (_buildingData != null && GridManager.Instance != null)
                GridManager.Instance.FreeArea(_gridOrigin, _buildingData.Size);

            EventBus<BuildingDestroyedEvent>.Publish(new BuildingDestroyedEvent(gameObject));
            LeanPool.Despawn(gameObject);
        }

        // Broadcasts the selection so the UI (Info Panel) can react.
        public virtual void OnSelected()
        {
            _highlight?.Highlight();
            EventBus<SelectionChangedEvent>.Publish(new SelectionChangedEvent(this));
        }

        // Visual deselection is delegated to SelectionHighlight; null-broadcast is handled by SelectionController.
        public virtual void OnDeselected()
        {
            _highlight?.ClearHighlight();
        }

        #endregion

        #region PRIVATE_METHODS

        // Lifecycle hook called once after Initialize() completes.
        // Override in subclasses to perform post-data-injection setup (e.g., spawn point resolution).
        protected virtual void OnInitialized() { }

        #endregion
    }
}
