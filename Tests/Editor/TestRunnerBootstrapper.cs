using ReaCS.Runtime.Registries;
using UnityEditor;

namespace ReaCS.Tests.Editor
{
    [InitializeOnLoad]
    public static class TestRunnerBootstrapper
    {
        static TestRunnerBootstrapper()
        {
#if UNITY_EDITOR
            ObservableRegistry.ClearAll();  // Ensure clean domain
#endif
        }
    }
}