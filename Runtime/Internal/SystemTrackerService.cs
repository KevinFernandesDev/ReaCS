using ReaCS.Runtime.Core;
using static ReaCS.Runtime.ReaCS;
using System.Collections.Generic;
using UnityEngine;

namespace ReaCS.Runtime.Internal
{
    public class SystemTracker : IReaCSService
    {
        private readonly HashSet<MonoBehaviour> _activeSystems = new();

        public void Register(MonoBehaviour system) => _activeSystems.Add(system);
        public void Unregister(MonoBehaviour system) => _activeSystems.Remove(system);

        public IEnumerable<MonoBehaviour> All => _activeSystems;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Reconnect()
        {
            foreach (var system in Use<SystemTracker>().All)
            {
                if (system is IReaCSReactiveSystem reactive && system.isActiveAndEnabled)
                    reactive.ForceResubscribe();
            }
        }
    }

    public interface IReaCSReactiveSystem
    {
        void ForceResubscribe();
    }
}
