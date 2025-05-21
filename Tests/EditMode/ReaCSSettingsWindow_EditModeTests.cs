using NUnit.Framework;
using ReaCS.Editor;
using UnityEditor;

namespace ReaCS.Tests.EditMode
{
    public class ReaCSSettingsWindow_EditModeTests
    {
        [Test]
        public void ShowWindow_Does_Not_Throw()
        {
            // Should open window without errors
            Assert.DoesNotThrow(() =>
            {
                ReaCSSettingsWindow.ShowWindow();
            });
        }

        [Test]
        public void DebugFlag_Can_Be_Toggled()
        {
            EditorPrefs.SetBool("ReaCS_DebugLogs", false);
            Assert.IsFalse(EditorPrefs.GetBool("ReaCS_DebugLogs"));

            EditorPrefs.SetBool("ReaCS_DebugLogs", true);
            Assert.IsTrue(EditorPrefs.GetBool("ReaCS_DebugLogs"));
        }
    }
}
