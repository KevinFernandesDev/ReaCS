using NUnit.Framework;
using ReaCS.Runtime.Core;
using ReaCS.Tests.Shared;
using System.Collections;
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

            // Simulate destruction
            ScriptableObject.DestroyImmediate(so);
            yield return null; // Let Update tick

            // Changing after destruction should not trigger
            bool triggered = false;
            so.OnChanged += (_, __) => triggered = true;

            yield return null;
            Assert.IsFalse(triggered, "SO should unregister on disable.");
        }

        [UnityTest]
        public IEnumerator CheckForChanges_Skips_If_Value_Unchanged()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "NoChange";
            so.ForceInitializeForTest();

            ObservableRuntimeWatcher.Register(so);

            bool triggered = false;
            so.OnChanged += (_, __) => triggered = true;

            // No change, wait one frame
            yield return null;
            Assert.IsFalse(triggered, "No change should not trigger OnChanged.");
        }

        [UnityTest]
        public IEnumerator SO_Change_Triggers_OnChanged()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "TestSO_OnChanged";
            so.ForceInitializeForTest();

            bool called = false;
            so.OnChanged += (obj, field) =>
            {
                if (field == nameof(so.number)) called = true;
            };

            ObservableRuntimeWatcher.Register(so);

            so.number.Value = 99;
            yield return null; // Wait for Update to tick

            Assert.IsTrue(called, "OnChanged should trigger on value change.");
        }

        [UnityTest]
        public IEnumerator No_OnChanged_When_Setting_Same_Value()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "NoTriggerSameValue";
            so.ForceInitializeForTest();

            so.number.Value = 123;
            ObservableRuntimeWatcher.Register(so);

            bool called = false;
            so.OnChanged += (_, __) => called = true;

            so.number.Value = 123; // same value
            yield return null;

            Assert.IsFalse(called, "OnChanged should not trigger when setting same value.");
        }

        [UnityTest]
        public IEnumerator Unregister_Stops_Further_Updates()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "UnregisterStop";
            so.ForceInitializeForTest();

            bool changed = false;
            so.OnChanged += (_, __) => changed = true;

            ObservableRuntimeWatcher.Register(so);
            ObservableRuntimeWatcher.Unregister(so);

            so.number.Value = 77;
            yield return null;

            Assert.IsFalse(changed, "Unregistered SO should not trigger OnChanged.");
        }

        [UnityTest]
        public IEnumerator Multiple_Changes_Trigger_Multiple_Times()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "MultipleChanges";
            so.ForceInitializeForTest();

            int triggerCount = 0;
            so.OnChanged += (_, __) => triggerCount++;

            ObservableRuntimeWatcher.Register(so);

            so.number.Value = 1;
            yield return null;
            so.number.Value = 2;
            yield return null;

            Assert.AreEqual(2, triggerCount, "OnChanged should trigger for each change.");
        }
    }
}
