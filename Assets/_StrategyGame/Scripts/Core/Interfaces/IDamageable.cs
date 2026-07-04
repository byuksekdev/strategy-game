namespace StrategyGame.Core
{
    // Every object that can take damage and be destroyed implements this interface.
    public interface IDamageable
    {
        int MaxHP { get; }
        int CurrentHP { get; }
        bool IsDead { get; }

        void TakeDamage(int amount);
        void Die();
    }
}
