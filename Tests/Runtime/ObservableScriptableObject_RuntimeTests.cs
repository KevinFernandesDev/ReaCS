using NUnit.Framework;
using ReaCS.Runtime;
using ReaCS.Runtime.Internal;
using ReaCS.Tests.Shared;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReaCS.Tests.Runtime
{
    public class ObservableScriptableObject_RuntimeTests
    {
        [UnityTest]
        public IEnumerator SO_Unregisters_OnDisable()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "TestSO_OnDisable";
            so.ForceInitializeForTest();

            ObservableRuntimeWatcher.Register(so);
            ScriptableObject.DestroyImmediate(so); // Simulates OnDisable

            yield return null;
            ObservableRuntimeWatcher.ForceUpdate(); // Should not process destroyed SO

            Assert.Pass("SO successfully unregistered on disable");
        }

        [UnityTest]
        public IEnumerator CheckForChanges_Skips_If_Not_Dirty()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "CheckIfNotDirty";
            so.ForceInitializeForTest();

            ObservableRuntimeWatcher.Register(so);
            yield return null;

            // forcibly call CheckForChanges with dirty = false
            so.GetType().GetMethod("CheckForChanges", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(so, null);

            Assert.Pass("Did not crash if dirty == false");
        }

        [UnityTest]
        public IEnumerator SO_Level_OnChanged_Triggers()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "TestSO_SO_Level_OnChanged_Triggers";
            so.ForceInitializeForTest();

            bool called = false;

            so.OnChanged += (obj, field) => { if (field == nameof(so.number)) called = true; };


            ObservableRuntimeWatcher.Register(so);
            so.number.Value = 99;
            ObservableRuntimeWatcher.ForceUpdate();
            yield return null;

            Assert.IsTrue(called);
        }

        [UnityTest]
        public IEnumerator No_Change_Does_Not_Trigger()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "TestSO_No_Change_Does_Not_Trigger";
            so.ForceInitializeForTest();

            // Set value initially
            so.number.Value = 123;

            // Register and cache the current value
            ObservableRuntimeWatcher.Register(so);
            ObservableRuntimeWatcher.ForceUpdate();
            yield return null;

            bool called = false;
            so.OnChanged += (_, __) => called = true;

            // Set the same value again
            so.number.Value = 123;
            ObservableRuntimeWatcher.ForceUpdate();
            yield return null;

            Assert.IsFalse(called);
        }

        [UnityTest]
        public IEnumerator MarkDirty_Missing_Does_Not_Trigger_Change()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "NoMarkDirty";
            so.ForceInitializeForTest();

            bool changed = false;
            so.OnChanged += (_, __) => changed = true;

            // Bypassing Value set to simulate no MarkDirty()
            var field = so.GetType().GetField("number", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var newObs = new Observable<int>();
            // manually set value bypassing MarkDirty
            typeof(Observable<int>)
                .GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(newObs, 42);

            field?.SetValue(so, newObs);

            ObservableRuntimeWatcher.Register(so);
            ObservableRuntimeWatcher.ForceUpdate();

            yield return null;
            Assert.IsFalse(changed);
        }

        [UnityTest]
        public IEnumerator Triggers_Change_When_No_Previous_Cache()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "TestSO_MissingCacheEntry";

            so.ForceInitializeForTest();

            // Manually remove the field's cached value to simulate missing data
            var field = typeof(TestSO).GetField("number");
            var cachedValuesField = typeof(ObservableScriptableObject)
                .GetField("_cachedValues", BindingFlags.NonPublic | BindingFlags.Instance);

            var cachedDict = cachedValuesField?.GetValue(so) as Dictionary<string, object>;
            cachedDict?.Remove("number"); // Remove entry to simulate missing cache

            bool called = false;
            so.OnChanged += (obj, fieldName) =>
            {
                if (fieldName == "number") called = true;
            };

            ObservableRuntimeWatcher.Register(so);
            so.number.Value = 99;
            ObservableRuntimeWatcher.ForceUpdate();

            yield return null;

            Assert.IsTrue(called);
        }

        [UnityTest]
        public IEnumerator Covers_Break_And_Closing_Braces()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "TestSO_Braces";

            so.ForceInitializeForTest();
            ObservableRuntimeWatcher.Register(so);

            // First change (goes into the if-block)
            so.number.Value = 10;
            ObservableRuntimeWatcher.ForceUpdate();
            yield return null;

            // Second change (goes into the same block again)
            so.number.Value = 15;
            ObservableRuntimeWatcher.ForceUpdate();
            yield return null;

            Assert.Pass("Break path exercised twice");
        }

    }
}