namespace StrategyGame.Units
{
    // Light soldier — lowest damage per strike (2).
    // All stats (HP, damage, speed) are overridable via the UnitData ScriptableObject.
    // BaseAttackDamage serves as the compile-time default when no UnitData is assigned.
    public class Soldier3 : UnitBase
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//

        //------Private Variables-------//
        protected override int BaseAttackDamage => 2;

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        #endregion
    }
}
