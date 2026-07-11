using System;

namespace StrategyGame.Core
{
    // Every object that can take damage and be destroyed implements this interface.
    public interface IDamageable
    {
        int MaxHP { get; }
        int CurrentHP { get; }
        bool IsDead { get; }

        // Fired by the implementing class whenever HP changes. Payload: (currentHP, maxHP).
        // Local subscribers (e.g. HealthBarView on the same GameObject) listen here
        // instead of a global event bus to avoid broadcasting every hit to the entire scene.
        event Action<int, int> OnHealthChanged;

        void TakeDamage(int amount);
        void Die();
    }
}
