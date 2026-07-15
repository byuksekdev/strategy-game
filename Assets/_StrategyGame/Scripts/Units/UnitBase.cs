using System;
using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using StrategyGame.Buildings;
using StrategyGame.Core;
using StrategyGame.Data;
using StrategyGame.Grid;
using StrategyGame.Pathfinding;

namespace StrategyGame.Units
{
    // Abstract base for all unit types (Soldier1, Soldier2, Soldier3).
    // Handles HP, A* movement (Coroutine), selection callbacks, and melee attack loops.
    //
    // Single Responsibility breakdown — each concern is owned by the right system:
    //   HP management      : this class (IDamageable contract)
    //   Selection visuals  : SelectionHighlight component (delegated via composition)
    //   UnitRegistry (init): UnitSpawnSystem.SpawnUnit() registers the unit after Initialize()
    //   UnitRegistry (death): UnitSpawnSystem subscribes to UnitDestroyedEvent and unregisters (SRP)
    //   UnitRegistry (move): MoveCoroutine updates the registry mid-step (inherently movement-coupled)
    //
    // Die() is a thin coordinator: it cancels active coroutines, publishes UnitDestroyedEvent
    // (carrying GridPosition so UnitSpawnSystem can unregister), and despawns.
    // UnitBase never calls UnitRegistry.Register/Unregister directly except during movement.
    //
    // IGridProvider is injected via Initialize() (method injection / DIP).
    // UnitBase only performs read operations on the grid (pathfinding, coordinate conversion),
    // so IGridProvider — not IGridService — is the correct minimal dependency (ISP).
    public abstract class UnitBase : MonoBehaviour, IDamageable, ISelectable, IInfoPanelProvider
    {
        //-------Public Variables-------//
        public int MaxHP => _unitData != null ? _unitData.MaxHP : BaseMaxHP;
        public int CurrentHP => _currentHP;
        public bool IsDead => _currentHP <= 0;
        public int AttackDamage => _unitData != null ? _unitData.AttackDamage : BaseAttackDamage;
        public float AttackCooldown => _unitData != null ? _unitData.AttackCooldown : BaseAttackCooldown;

        public UnitData Data => _unitData;
        public Vector2Int GridPosition => _gridPosition;

        // IInfoPanelProvider — allows InformationPanelController to display unit info
        // without a concrete UnitBase type-check. UnitData is a subtype of EntityData.
        public EntityData EntityData => _unitData;
        public bool CanBeDeleted => false;
        public IUnitProducer UnitProducer => null;

        // Fired whenever HP changes. HealthBarView and InformationPanelController
        // subscribe directly instead of listening to a global event bus.
        public event Action<int, int> OnHealthChanged;

        //------Serialized Fields-------//
        [Tooltip("Selection highlight component (tints the sprite when selected).")]
        [SerializeField] private SelectionHighlight _highlight;

        //------Private Variables-------//
        private UnitData _unitData;
        private int _currentHP;
        private Vector2Int _gridPosition;

        // Injected once via Initialize(); used for all grid read operations.
        // Typed as IGridProvider: units only need read access (ISP).
        private IGridProvider _gridProvider;

        // Outer coroutine handle (MoveActionCoroutine or AttackCoroutine).
        private Coroutine _activeCoroutine;
        // Inner MoveCoroutine handle — must be stopped explicitly because Unity does NOT
        // automatically stop child coroutines when their parent coroutine is stopped.
        private Coroutine _innerMoveCoroutine;
        // The grid cell reserved (registered) for the current lerp step.
        // Null when the unit is not mid-step. Used by CancelActiveAction to
        // restore registry consistency when movement is interrupted.
        private Vector2Int? _claimedNextCell;

        // Subclass defaults — used as fallback when no UnitData is injected.
        protected virtual int BaseMaxHP => 10;
        protected virtual int BaseAttackDamage => 1;
        protected virtual float BaseAttackCooldown => 1f;

        #region UNITY_METHODS

        private void Awake()
        {
            if (_highlight == null)
                _highlight = GetComponent<SelectionHighlight>();
        }

        #endregion

        #region PUBLIC_METHODS

        // Called by UnitSpawnSystem after LeanPool.Spawn to inject runtime data and grid provider.
        // gridProvider gives read-only grid access for coordinate conversion and pathfinding.
        // UnitRegistry.Register is NOT called here — UnitSpawnSystem owns that responsibility (SRP).
        public void Initialize(UnitData data, Vector2Int startCell, IGridProvider gridProvider)
        {
            _gridProvider = gridProvider;
            _unitData = data;
            _currentHP = MaxHP;
            _gridPosition = startCell;

            OnHealthChanged?.Invoke(_currentHP, MaxHP);

            Vector3 worldPos = _gridProvider != null
                ? _gridProvider.GridToWorld(startCell)
                : transform.position;

            transform.position = worldPos;
        }

        // Moves the unit to the target cell using A*. Cancels any ongoing action.
        public void MoveTo(Vector2Int targetCell)
        {
            CancelActiveAction();
            _activeCoroutine = StartCoroutine(MoveActionCoroutine(targetCell));
        }

        // Moves adjacent to any cell in the target's footprint and attacks repeatedly.
        // Accepts a list of cells so that multi-cell buildings (e.g. 4×4 Barracks) are
        // handled correctly — free neighbours are searched across the entire footprint, not
        // just the single clicked cell. Cancels any ongoing action before starting.
        public void AttackTarget(IReadOnlyList<Vector2Int> targetCells, IDamageable target)
        {
            CancelActiveAction();
            _activeCoroutine = StartCoroutine(AttackCoroutine(targetCells, target));
        }

        public void TakeDamage(int amount)
        {
            if (_currentHP <= 0 || amount <= 0) return;

            _currentHP = Mathf.Max(0, _currentHP - amount);
            OnHealthChanged?.Invoke(_currentHP, MaxHP);
            _highlight?.FlashDamage();

            if (_currentHP <= 0) Die();
        }

        // Cancels active coroutines, publishes UnitDestroyedEvent (carrying GridPosition so that
        // UnitSpawnSystem can unregister from UnitRegistry — SRP), then returns to the pool.
        public virtual void Die()
        {
            CancelActiveAction();
            EventBus.Publish(new UnitDestroyedEvent(gameObject, _gridPosition));
            LeanPool.Despawn(gameObject);
        }

        // Updates own visual state only. SelectionController is responsible for
        // publishing SelectionChangedEvent so that UI reacts (MVC: Model ≠ Controller).
        public virtual void OnSelected()
        {
            _highlight?.Highlight();
        }

        public virtual void OnDeselected()
        {
            _highlight?.ClearHighlight();
        }

        #endregion

        #region PRIVATE_METHODS

        // Top-level wrapper so standalone MoveTo clears _activeCoroutine when done.
        // Stores the inner MoveCoroutine handle so CancelActiveAction can stop it too.
        private IEnumerator MoveActionCoroutine(Vector2Int targetCell)
        {
            _innerMoveCoroutine = StartCoroutine(MoveCoroutine(targetCell));
            yield return _innerMoveCoroutine;
            _innerMoveCoroutine = null;
            _activeCoroutine    = null;
        }

        // Walks the unit along an A* path to the target cell, one cell per step.
        // _gridPosition stays at the COMPLETED cell until the lerp finishes so that
        // cancelling mid-step restores the unit to its last known grid cell correctly.
        // Recalculates the path dynamically if a cell becomes occupied mid-movement.
        private IEnumerator MoveCoroutine(Vector2Int targetCell)
        {
            IGridProvider grid = _gridProvider;
            if (grid == null) yield break;

            List<Vector2Int> path = AStarPathfinder.FindPath(_gridPosition, targetCell, grid);
            if (path == null || path.Count == 0) yield break;

            int index = 0;
            while (index < path.Count)
            {
                Vector2Int nextCell = path[index];

                // If the next cell was claimed by another unit since path was computed,
                // recalculate from the current grid position rather than walking into a conflict.
                if (UnitRegistry.IsCellOccupied(nextCell) && nextCell != targetCell)
                {
                    path = AStarPathfinder.FindPath(_gridPosition, targetCell, grid);
                    if (path == null || path.Count == 0) yield break;
                    index = 0;
                    continue;
                }

                // Reserve the destination cell in the registry so other units route around it.
                // _gridPosition is intentionally NOT updated yet; it reflects the last COMPLETED
                // step. CancelActiveAction uses this to restore registry state correctly.
                Vector2Int fromCell  = _gridPosition;
                _claimedNextCell     = nextCell;
                UnitRegistry.Unregister(fromCell);
                UnitRegistry.Register(this, nextCell);

                Vector3 startPos = transform.position;
                Vector3 endPos   = grid.GridToWorld(nextCell);
                // Use actual distance so diagonal steps (≈ √2 × cell) maintain consistent speed.
                float dist     = Vector3.Distance(startPos, endPos);
                float duration = dist / (_unitData != null ? _unitData.MoveSpeed : 3f);
                float elapsed  = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                    yield return null;
                }

                transform.position = endPos;
                _gridPosition      = nextCell; // mark step complete
                _claimedNextCell   = null;
                index++;
            }
        }

        // Moves adjacent to the target's footprint then attacks repeatedly (cooldown between
        // strikes) until the target is dead, despawned, or the action is cancelled externally.
        private IEnumerator AttackCoroutine(IReadOnlyList<Vector2Int> targetCells, IDamageable target)
        {
            IGridProvider grid = _gridProvider;
            if (grid == null) yield break;

            // Search all cells in the target's footprint for a free adjacent cell.
            // This correctly handles multi-cell buildings: an interior cell of a 4×4 Barracks
            // has no walkable direct neighbours, but the building's border cells do.
            Vector2Int? adjacentCell = FindAdjacentFreeCell(targetCells, grid);
            if (!adjacentCell.HasValue) yield break;

            // Move to the adjacent cell (store handle so CancelActiveAction can stop it).
            _innerMoveCoroutine = StartCoroutine(MoveCoroutine(adjacentCell.Value));
            yield return _innerMoveCoroutine;
            _innerMoveCoroutine = null;

            // Attack loop: keep striking until the target is dead or no longer active.
            // IsTargetActive guards against LeanPool object reuse: Despawn sets the
            // GameObject inactive, so a deactivated target is treated as gone even if
            // its IsDead check hasn't been reached yet.
            var cooldownWait = new WaitForSeconds(AttackCooldown);
            while (!target.IsDead && IsTargetActive(target))
            {
                target.TakeDamage(AttackDamage);

                if (target.IsDead || !IsTargetActive(target)) break;

                yield return cooldownWait;
            }

            _activeCoroutine = null;
        }

        // Searches the cardinal neighbours of EVERY cell in the target's footprint and
        // returns the first that is both walkable and free of other units.
        // A visited set prevents checking the same neighbour cell twice when footprint
        // cells share edges (common in multi-cell buildings).
        private static Vector2Int? FindAdjacentFreeCell(IReadOnlyList<Vector2Int> targetCells, IGridProvider grid)
        {
            var visited = new HashSet<Vector2Int>();

            foreach (Vector2Int targetCell in targetCells)
            {
                GridCell cell = grid.GetCell(targetCell);
                if (cell == null) continue;

                foreach (GridCell neighbour in grid.GetNeighbors(cell))
                {
                    if (!visited.Add(neighbour.Coordinate)) continue;

                    if (neighbour.IsWalkable && !UnitRegistry.IsCellOccupied(neighbour.Coordinate))
                        return neighbour.Coordinate;
                }
            }

            return null;
        }

        // Returns true when the target is a live (active) MonoBehaviour.
        // LeanPool.Despawn sets the GameObject inactive, so this guards against
        // the object pool reuse scenario where IsDead would otherwise be stale.
        private static bool IsTargetActive(IDamageable target)
            => target is MonoBehaviour mb && mb.gameObject.activeInHierarchy;

        private void CancelActiveAction()
        {
            // Stop inner MoveCoroutine first — Unity does NOT propagate StopCoroutine
            // to child coroutines started with StartCoroutine(), so we must stop it explicitly.
            if (_innerMoveCoroutine != null)
            {
                StopCoroutine(_innerMoveCoroutine);
                _innerMoveCoroutine = null;
            }

            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }

            // Restore registry consistency when cancelled mid-step:
            // unregister the claimed-ahead cell and re-register at the last completed position.
            if (_claimedNextCell.HasValue)
            {
                UnitRegistry.Unregister(_claimedNextCell.Value);
                UnitRegistry.Register(this, _gridPosition);
                _claimedNextCell = null;
            }
        }

        #endregion
    }
}
