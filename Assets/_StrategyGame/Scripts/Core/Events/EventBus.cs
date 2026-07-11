using System;

namespace StrategyGame.Core
{
    // Generic publish-subscribe bus for cross-system communication.
    // Each event type gets its own isolated static channel via the type parameter,
    // so EventBus<BuildingPlacedEvent> and EventBus<UnitSpawnedEvent> never interfere.
    //
    // Subscribe in OnEnable, Unsubscribe in OnDisable.
    // This ensures pooled objects and panel-toggled MonoBehaviours never leak references.
    public static class EventBus<T> where T : struct
    {
        private static event Action<T> OnEvent;

        public static void Subscribe(Action<T> listener)   => OnEvent += listener;
        public static void Unsubscribe(Action<T> listener) => OnEvent -= listener;
        public static void Publish(T eventData)            => OnEvent?.Invoke(eventData);
    }
}
