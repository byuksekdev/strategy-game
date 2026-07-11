namespace StrategyGame.Units
{
    // Medium soldier — mid-tier damage per strike (5).
    // All stats (HP, damage, speed) are overridable via the UnitData ScriptableObject.
    // BaseAttackDamage serves as the compile-time default when no UnitData is assigned.
    public class Soldier2 : UnitBase
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//

        //------Private Variables-------//
        protected override int BaseAttackDamage => 5;

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        #endregion
    }
}
