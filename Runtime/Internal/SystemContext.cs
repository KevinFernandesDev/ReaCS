using System;

namespace ReaCS.Runtime
{
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