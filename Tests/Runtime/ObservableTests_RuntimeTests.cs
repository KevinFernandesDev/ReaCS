using NUnit.Framework;
using ReaCS.Runtime;
using ReaCS.Tests.Shared;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReaCS.Tests.Runtime
{
    public class ObservableTests_RuntimeTests
    {
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
    }
}