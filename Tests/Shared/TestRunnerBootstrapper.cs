using ReaCS.Runtime;
using UnityEditor;

namespace ReaCS.Tests.Shared
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