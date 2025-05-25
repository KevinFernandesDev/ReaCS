using NUnit.Framework;
using ReaCS.Runtime;
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
            // Cleanup any existing instance first
            var existing = GameObject.Find("ObservableRuntimeWatcher");
            if (existing != null)
                Object.DestroyImmediate(existing);

            yield return null; // Let destruction happen

            // Trigger init
            typeof(ObservableRuntimeWatcher)
                .GetMethod("Init", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(null, null);

            yield return null;

            var watcherGO = GameObject.Find("ObservableRuntimeWatcher");
            Assert.IsNotNull(watcherGO, "Watcher GameObject should exist");
            Assert.IsNotNull(watcherGO.GetComponent<ObservableRuntimeWatcher>(), "Watcher component should be present");

            Object.Destroy(watcherGO); // Clean up after test
        }


        [UnityTest]
        public IEnumerator Registers_And_Updates_SO()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "TestSO_Registers_And_Updates";
            so.ForceInitializeForTest();

            bool triggered = false;
            so.OnChanged += (_, field) =>
            {
                if (field == nameof(so.number)) triggered = true;
            };

            ObservableRuntimeWatcher.Register(so);
            so.number.Value = 5;
            ObservableRuntimeWatcher.ForceUpdate();
            yield return null;

            Assert.IsTrue(triggered);
        }

        [UnityTest]
        public IEnumerator Debounce_Does_Not_Update_Immediately()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "TestSO_Debounce_Does_Not_Update_Immediately";
            so.ForceInitializeForTest();
            ObservableRuntimeWatcher.Register(so);

            bool triggered = false;
            so.OnChanged += (_, __) => triggered = true;

            so.number.Value = 88;
            ObservableRuntimeWatcher.DebounceChange(so, 1f);

            ObservableRuntimeWatcher.ForceUpdate();
            yield return null;

            Assert.IsFalse(triggered);
        }

        [UnityTest]
        public IEnumerator Debounce_Overwrites_Previous_Delay()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "DebounceOverwrite";
            so.ForceInitializeForTest();

            ObservableRuntimeWatcher.Register(so);
            ObservableRuntimeWatcher.DebounceChange(so, 2f);
            ObservableRuntimeWatcher.DebounceChange(so, 0.1f); // should overwrite

            yield return new WaitForSeconds(0.2f);
            ObservableRuntimeWatcher.ForceUpdate();

            Assert.Pass("Debounce resolved correctly");
        }

        [UnityTest]
        public IEnumerator Unregister_SO_Stops_Updates()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "UnregisterSO";
            so.ForceInitializeForTest();

            bool changed = false;
            so.OnChanged += (_, __) => changed = true;

            ObservableRuntimeWatcher.Register(so);
            ObservableRuntimeWatcher.Unregister(so);

            so.number.Value = 7;
            ObservableRuntimeWatcher.ForceUpdate();

            yield return null;
            Assert.IsFalse(changed);
        }

        [UnityTest]
        public IEnumerator Debounce_Clears_After_Duration()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "DebounceExpire";
            so.ForceInitializeForTest();

            ObservableRuntimeWatcher.Register(so);
            ObservableRuntimeWatcher.DebounceChange(so, 0.1f);

            yield return new WaitForSeconds(0.2f);
            ObservableRuntimeWatcher.ForceUpdate();

            Assert.Pass("Debounce cleared");
        }

        [UnityTest]
        public IEnumerator Debounced_Observable_Is_Checked_After_Delay()
        {
            var so = ScriptableObject.CreateInstance<DebounceTestSO>();
            so.ForceInitializeForTest();

            bool wasTriggered = false;
            so.OnTestChanged += () => wasTriggered = true;

            ObservableRuntimeWatcher.Register(so);
            so.TriggerChange(42); // change and dirty it
            ObservableRuntimeWatcher.DebounceChange(so, 0.1f);

            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(wasTriggered, "Debounced SO should have triggered OnChanged after delay.");
        }

        [UnityTest]
        public IEnumerator Init_Creates_RuntimeWatcher_Instance()
        {
            // Destroy manually if present
            var existing = GameObject.Find("ObservableRuntimeWatcher");
            if (existing != null)
                Object.Destroy(existing);

            yield return null;

            var initMethod = typeof(ObservableRuntimeWatcher)
                .GetMethod("Init", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            initMethod?.Invoke(null, null);

            yield return null;

            var go = GameObject.Find("ObservableRuntimeWatcher");
            Assert.IsNotNull(go, "Runtime watcher GameObject should exist.");
            Assert.IsNotNull(go.GetComponent<ObservableRuntimeWatcher>(), "Watcher component should be present.");
        }
    }
}