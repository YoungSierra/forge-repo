using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProfessorSprat.Core
{
    /// <summary>
    /// Typed synchronous publish/subscribe for every cross-mechanic event (TDD §B-S EventBus). Handlers run in subscription
    /// order inside the publishing call, so a chain (landing → stomp → crab defeat → bounce) resolves in one frame.
    /// </summary>
    public static class EventBus
    {
        #region Fields

        private static readonly Dictionary<Type, Delegate> Handlers = new Dictionary<Type, Delegate>();

        #endregion

        #region Public Methods

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Handlers.TryGetValue(typeof(T), out Delegate existing);
            Handlers[typeof(T)] = Delegate.Combine(existing, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (!Handlers.TryGetValue(typeof(T), out Delegate existing))
            {
                return;
            }

            Delegate remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
            {
                Handlers.Remove(typeof(T));
            }
            else
            {
                Handlers[typeof(T)] = remaining;
            }
        }

        public static void Publish<T>(T payload) where T : struct
        {
            if (Handlers.TryGetValue(typeof(T), out Delegate handler))
            {
                ((Action<T>)handler).Invoke(payload);
            }
        }

        /// <summary>Removes every subscription (scene start, tests).</summary>
        public static void Clear()
        {
            Handlers.Clear();
        }

        #endregion

        #region Private Methods

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Handlers.Clear();
        }

        #endregion
    }
}
