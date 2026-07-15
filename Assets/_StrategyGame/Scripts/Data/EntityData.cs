using UnityEngine;

namespace StrategyGame.Data
{
    // Common data for all game entities that can be produced (building, unit, etc.).
    // BuildingData and UnitData inherit from this class.
    //
    // IProducible: data assets are themselves producible items — Data returns 'this'.
    // This lets the production menu, InfiniteScrollView, and related systems depend only
    // on IProducible, never on a concrete data type (DIP / OCP).
    public abstract class EntityData : ScriptableObject, IProducible
    {
        //-------Public Variables-------//
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;

        // IProducible — the data asset is its own producible descriptor.
        EntityData IProducible.Data => this;

        //------Serialized Fields-------//
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        //------Private Variables-------//

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        #endregion
    }
}
