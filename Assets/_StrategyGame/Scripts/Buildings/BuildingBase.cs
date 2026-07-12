using System;
using Lean.Pool;
using UnityEngine;
using StrategyGame.Core;
using StrategyGame.Data;
using StrategyGame.Grid;

namespace StrategyGame.Buildings
{
    // Abstract base for all buildings (Barracks, PowerPlant, etc.).
    //
    // Single Responsibility breakdown — each concern is owned by the right system:
    //   HP management    : this class (IDamageable contract)
    //   Selection visuals: SelectionHighlight component (delegated via composition)
    //   Grid cleanup     : GridManager subscribes to BuildingDestroyedEvent (event-driven, SRP)
    //   Object pooling   : LeanPool.Despawn — lifecycle infrastructure call in Die()
    //
    // Die() is now a thin coordinator: it publishes BuildingDestroyedEvent (which carries
    // the grid data GridManager needs) and returns the object to the pool.
    // BuildingBase no longer stores or calls into IGridOccupancyManager directly (DIP + SRP).
    //
    // IGridProvider is injected via Initialize() so subclasses that need read-only grid
    // access (e.g. Barracks resolving its spawn point) can use GridProvider (ISP).
    public abstract class BuildingBase : MonoBehaviour, IDamageable, ISelectable, IProducible, IInfoPanelProvider
    {
        //-------Public Variables-------//
        public int MaxHP => _buildingData != null ? _buildingData.MaxHP : 0;
        public int CurrentHP => _currentHP;
        public bool IsDead => _currentHP <= 0;

        // IProducible — used by the production menu to retrieve display data.
        public EntityData Data => _buildingData;

        // IInfoPanelProvider — used by InformationPanelController without casting to a concrete type.
        public EntityData EntityData => _buildingData;
        public bool CanBeDeleted => true;

        // Returns this as IUnitProducer when the concrete subclass (e.g. Barracks) implements it;
        // null for non-producing buildings (e.g. PowerPlant). InformationPanelController reads this
        // to decide whether to show the production list, without knowing the concrete type.
        public virtual IUnitProducer UnitProducer => this as IUnitProducer;

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

        // Read-only grid access for subclasses (ISP: no write operations needed by BuildingBase).
        // Grid cleanup on death is delegated to GridManager via BuildingDestroyedEvent (SRP).
        protected IGridProvider GridProvider { get; private set; }

        private SelectionHighlight _highlight;

        #region UNITY_METHODS

        private void Awake()
        {
            _highlight = GetComponent<SelectionHighlight>();
        }

        #endregion

        #region PUBLIC_METHODS

        // Called by BuildingFactory after instantiation to inject runtime data and grid position.
        // IGridProvider is sufficient here — grid cleanup is now handled by GridManager via events,
        // so BuildingBase no longer needs write access to the grid (ISP + SRP).
        public void Initialize(BuildingData data, Vector2Int gridOrigin, IGridProvider gridProvider)
        {
            GridProvider = gridProvider;
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

        // Publishes BuildingDestroyedEvent (which carries grid data for GridManager to free the area)
        // then returns the object to the pool. BuildingBase has no direct grid write dependency.
        public virtual void Die()
        {
            EventBus.Publish(
                new BuildingDestroyedEvent(gameObject, _gridOrigin, _buildingData?.Size ?? Vector2Int.zero));

            LeanPool.Despawn(gameObject);
        }

        // Updates own visual state only. SelectionController is responsible for
        // publishing SelectionChangedEvent so that UI reacts (MVC: Model ≠ Controller).
        public virtual void OnSelected()
        {
            _highlight?.Highlight();
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
