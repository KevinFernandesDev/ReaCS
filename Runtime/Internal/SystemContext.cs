using System;

namespace ReaCS.Runtime.Internal
{
    /// <summary>
    /// Temporarily track the name of the system currently running.
    /// This value is then recorded in LogToBurstHistory(...)
    /// So your graph/debugger/history can say: “This value was changed by 'MySystem'.”
    /// It's used by runtime debug graphs, history debug and is a way to label cause chains.
    /// It's thread-safe for jobs & parallel processing, and fast (no lookup or instantiation)
    /// </summary>
    public static class SystemContext
    {
        [ThreadStatic] private static string _activeSystemName;
        public static string ActiveSystemName => _activeSystemName;

        public static void Push(string systemName) => _activeSystemName = systemName;
        public static void Pop() => _activeSystemName = null;
        public static void WithSystem(string name, Action action)
        {
            Push(name);
            try { action(); }
            finally { Pop(); }
        }
    }
}