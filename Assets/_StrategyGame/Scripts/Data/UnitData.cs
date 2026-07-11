using UnityEngine;

namespace StrategyGame.Data
{
    // Data for all unit types (Soldier 1/2/3).
    [CreateAssetMenu(fileName = "UnitData", menuName = "StrategyGame/Data/Unit Data")]
    public class UnitData : EntityData
    {
        //-------Public Variables-------//
        public int MaxHP => _maxHP;
        public int AttackDamage => _attackDamage;
        public float MoveSpeed => _moveSpeed;
        public float AttackCooldown => _attackCooldown;
        public GameObject Prefab => _prefab;

        //------Serialized Fields-------//
        [Header("Stats")]
        [SerializeField] private int _maxHP = 10;
        [SerializeField] private int _attackDamage = 10;
        [SerializeField] private float _moveSpeed = 3f;
        [Tooltip("Seconds between consecutive attacks while in melee range.")]
        [SerializeField] private float _attackCooldown = 1f;

        [Header("Prefab")]
        [SerializeField] private GameObject _prefab;

        //------Private Variables-------//

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        #endregion

        #region PRIVATE_METHODS

        #endregion
    }
}
