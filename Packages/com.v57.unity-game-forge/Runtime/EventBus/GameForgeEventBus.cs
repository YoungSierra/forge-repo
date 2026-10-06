using System;
using System.Collections.Generic;

namespace V57.GameForge
{
    /// <summary>
    /// Minimal pub/sub bus for graybox Unity prototypes.
    /// </summary>
    public static class GameForgeEventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> Handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                Handlers[type] = list;
            }

            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (!Handlers.TryGetValue(typeof(T), out var list))
                return;
            list.Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            if (!Handlers.TryGetValue(typeof(T), out var list))
                return;
            // Copy to allow unsubscribe during publish
            var snapshot = list.ToArray();
            foreach (var d in snapshot)
            {
                if (d is Action<T> action)
                    action(evt);
            }
        }

        public static void Clear() => Handlers.Clear();
    }
}
