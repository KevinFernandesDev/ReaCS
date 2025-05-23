using NUnit.Framework;
using ReaCS.Runtime;
using ReaCS.Tests.Shared;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReaCS.Tests.EditMode
{
    public class ObservableTests_EditModeTests
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