using ReaCS.Runtime.Core;
using ReaCS.Runtime.Internal;
using UnityEngine;

namespace ReaCS.Runtime
{
    /// <summary>
    /// This is made to be able to not use Domain Reload and get faster workflow
    /// while maintaining the correct behavior for some specific systems
    /// </summary>
    public static class ReaCSStartup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitAll()
        {
            ReaCSBurstHistory.Init();
            ObservableRuntimeWatcher.Init(); // if not already
        }
    }
}
