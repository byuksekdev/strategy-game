using UnityEngine;
using StrategyGame.Buildings;
using StrategyGame.Grid;
using StrategyGame.Units;

namespace StrategyGame.Core
{
    // Composition root: the single place in the codebase that knows about concrete
    // implementations and wires them into their consumers via interface injection.
    //
    // Why a separate bootstrapper?
    //   • DIP requires that consumers depend on abstractions, not concretions.
    //   • Without a composition root, every MonoBehaviour would need to call
    //     GridManager.Instance directly, coupling them to the concrete class.
    //   • Here, only GameBootstrapper imports GridManager. All other systems
    //     receive IGridService / IGridProvider / IBuildingFactory through Inject().
    //
    // Execution order:
    //   All Awake() calls (including GridManager.Awake) complete before any Start().
    //   Therefore Start() is the correct place to resolve and inject dependencies.
    public class GameBootstrapper : MonoBehaviour
    {
        //------Serialized Fields-------//
        [Header("Systems — assign in Inspector")]
        [SerializeField] private BuildingPlacementController _placementController;
        [SerializeField] private SelectionController _selectionController;
        [SerializeField] private UnitSpawnSystem _unitSpawnSystem;
        [SerializeField] private GridVisualizer _gridVisualizer;

        #region UNITY_METHODS

        private void Start()
        {
            IGridService grid = GridManager.Instance;

            if (grid == null)
            {
                Debug.LogError("[GameBootstrapper] GridManager not found in scene. " +
                               "Ensure a GridManager component exists and its Awake() has run.");
                return;
            }

            // BuildingFactory needs full IGridService: reads area info AND writes occupancy.
            IBuildingFactory buildingFactory = new BuildingFactory(grid);

            // Each consumer receives only the interface it genuinely needs (ISP).
            _placementController?.Inject(buildingFactory);   // factory handles writes
            _placementController?.Inject((IGridProvider)grid); // placement reads grid for preview

            _selectionController?.Inject((IGridProvider)grid); // selection reads grid only

            _unitSpawnSystem?.Inject((IGridProvider)grid);     // spawn BFS reads grid only

            _gridVisualizer?.Inject((IGridProvider)grid);      // visualizer reads grid dimensions only
        }

        // Clears UnitRegistry when this bootstrapper is destroyed (e.g. runtime scene reload).
        // StaticStateResetter handles the play-session start via [RuntimeInitializeOnLoadMethod],
        // but SceneManager.LoadScene at runtime does NOT trigger that attribute again — so an
        // explicit clear here guarantees a clean slate after any in-game scene transition.
        private void OnDestroy()
        {
            UnitRegistry.Clear();
        }

        #endregion
    }
}
