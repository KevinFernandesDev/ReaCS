using NUnit.Framework;
using ReaCS.Runtime.Core;
using ReaCS.Tests.Shared;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReaCS.Tests.Runtime
{
    public class ObservableRuntimeWatcher_RuntimeTests
    {
        [UnityTest]
        public IEnumerator RuntimeWatcher_Initializes_OnLoad()
        {
            var existing = GameObject.Find("ObservableRuntimeWatcher");
            if (existing != null)
                Object.DestroyImmediate(existing);

            yield return null;

            typeof(ObservableRuntimeWatcher)
                .GetMethod("Init", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(null, null);

            yield return null;

            var watcherGO = GameObject.Find("ObservableRuntimeWatcher");
            Assert.IsNotNull(watcherGO);
            Assert.IsNotNull(watcherGO.GetComponent<ObservableRuntimeWatcher>());
        }

        [UnityTest]
        public IEnumerator SO_Change_Triggers_OnChanged_After_Update()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.ForceInitializeForTest();

            bool triggered = false;
            so.OnChanged += (_, field) =>
            {
                if (field == nameof(so.number)) triggered = true;
            };

            ObservableRuntimeWatcher.Register(so);

            so.number.Value = 42;
            yield return null; // let Update run

            Assert.IsTrue(triggered);
        }

        [UnityTest]
        public IEnumerator Unregister_SO_Stops_Further_Updates()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.ForceInitializeForTest();

            bool changed = false;
            so.OnChanged += (_, __) => changed = true;

            ObservableRuntimeWatcher.Register(so);
            ObservableRuntimeWatcher.Unregister(so);

            so.number.Value = 7;
            yield return null; // let Update run

            Assert.IsFalse(changed, "Unregistered SO should not trigger OnChanged.");
        }
    }
}
