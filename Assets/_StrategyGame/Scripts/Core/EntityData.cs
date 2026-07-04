using UnityEngine;

namespace StrategyGame.Data
{
    // Common data for all game entities that can be produced (building, unit, etc.).
    // BuildingData and UnitData inherit from this class.
    public abstract class EntityData : ScriptableObject
    {
        //-------Public Variables-------//
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;

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
