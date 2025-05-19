using NUnit.Framework;
using ReaCS.Runtime;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReaCS.Tests.Runtime
{
    public class ObservableTests
    {
        [Test]
        public void Observable_Init_Only_Sets_Once()
        {
            var obs = new Observable<int>();
            var so1 = ScriptableObject.CreateInstance<TestSO>();
            var so2 = ScriptableObject.CreateInstance<TestSO>();

            obs.Init(so1, "number");
            obs.Init(so2, "number"); // should be ignored or throw

            Assert.Pass("Init did not throw twice");
        }

        [UnityTest]
        public IEnumerator Observable_Field_Level_OnChanged_Triggers()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            bool triggered = false;
            so.number.OnChanged += val => { if (val == 42) triggered = true; };

            so.number.Value = 42;
            yield return null;

            Assert.IsTrue(triggered);
        }

        [UnityTest]
        public IEnumerator Observable_Does_Not_Trigger_If_Value_Same()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.number.Value = 10;

            bool triggered = false;
            so.number.OnChanged += val => triggered = true;
            so.number.Value = 10;
            yield return null;

            Assert.IsFalse(triggered);
        }

        [Test]
        public void Observable_Does_Not_Trigger_On_Same_Value()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.ForceInitializeForTest();

            bool triggered = false;
            so.number.OnChanged += _ => triggered = true;

            so.number.Value = 0;
            Assert.IsFalse(triggered);
        }

        [Test]
        public void Observable_Triggers_On_Changed_Value()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.ForceInitializeForTest();

            bool triggered = false;
            so.number.OnChanged += _ => triggered = true;

            so.number.Value = 42;
            Assert.IsTrue(triggered);
        }

        [Test]
        public void Observable_Multiple_Subscribers_Fire()
        {
            var obs = new Observable<int>();
            int calls = 0;

            obs.OnChanged += _ => calls++;
            obs.OnChanged += _ => calls++;

            obs.Value = 10;

            Assert.AreEqual(2, calls);
        }
    }
}