using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame.Core
{
    // Generic publish-subscribe bus for cross-system communication.
    // Each event type gets its own isolated channel keyed by Type, so
    // Subscribe<BuildingPlacedEvent> never interfere.
    //
    // Subscribe in OnEnable, Unsubscribe in OnDisable.
    // This ensures pooled objects and panel-toggled MonoBehaviours never leak references.
    //
    // Lifecycle: [RuntimeInitializeOnLoadMethod] fires before any scene loads, clearing all
    // channels automatically — even when "Reload Domain" is disabled in Project Settings.
    // No external StaticStateResetter needed; new event types are discovered automatically
    // at runtime the first time they are subscribed or published.
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _channels = new();

        // Runs before any scene loads AND when the domain reloads.
        // Works correctly because EventBus is now a non-generic static class.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload() => _channels.Clear();

        public static void Subscribe<T>(Action<T> listener) where T : struct
        {
            var type = typeof(T);
            _channels[type] = _channels.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, listener)
                : listener;
        }

        public static void Unsubscribe<T>(Action<T> listener) where T : struct
        {
            var type = typeof(T);
            if (!_channels.TryGetValue(type, out var existing)) return;

            var updated = Delegate.Remove(existing, listener);
            if (updated == null)
                _channels.Remove(type);
            else
                _channels[type] = updated;
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            if (!_channels.TryGetValue(typeof(T), out var d)) return;

            var invocationList = d.GetInvocationList();
            foreach (var handler in invocationList)
            {
                try
                {
                    ((Action<T>)handler).Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Subscriber threw an exception for event '{typeof(T).Name}': {ex}");
                }
            }
        }
    }
}
