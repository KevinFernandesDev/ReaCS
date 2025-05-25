using NUnit.Framework;
using UnityEngine;
using ReaCS.Runtime;
using ReaCS.Tests.Shared;
using System.Linq;
using ReaCS.Runtime.Core;

namespace ReaCS.Tests.EditMode
{
    public class SystemBase_EditModeTests
    {
        [Test]
        public void ReactToAttribute_Is_Correct()
        {
            var attr = typeof(TestSystem).GetCustomAttributes(typeof(ReactToAttribute), true);
            Assert.IsNotEmpty(attr);
            Assert.AreEqual("number", ((ReactToAttribute)attr[0]).FieldName);
        }

        [Test]
        public void IsTarget_CanBeOverridden()
        {
            var system = new GameObject().AddComponent<TestSystem>();
            var testSO = ScriptableObject.CreateInstance<TestSO>();
            testSO.name = "Allow";

            var method = typeof(TestSystem).GetMethod("IsTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method.Invoke(system, new object[] { testSO });

            Assert.IsTrue(result);

            Object.DestroyImmediate(testSO);
            Object.DestroyImmediate(system.gameObject);
        }

        [Test]
        public void Registry_Adds_And_Removes_SO()
        {
            var testSO = ScriptableObject.CreateInstance<TestSO>();
            testSO.ForceInitializeForTest();

            ObservableRegistry.Register(testSO);
            var list = ObservableRegistry.GetAll<TestSO>();
            Assert.Contains(testSO, (System.Collections.ICollection)list);

            ObservableRegistry.Unregister(testSO);
            list = ObservableRegistry.GetAll<TestSO>();
            Assert.IsFalse(list.Contains(testSO));

            Object.DestroyImmediate(testSO);
        }

        [Test]
        public void ReactToAttributeIsRequired()
        {
            var attr = typeof(TestSystem).GetCustomAttributes(typeof(ReactToAttribute), true);
            Assert.IsNotEmpty(attr);
            Assert.AreEqual("number", ((ReactToAttribute)attr[0]).FieldName);
        }

        [Test]
        public void System_With_No_ReactTo_Does_Not_React()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.number.Init(so, nameof(so.number));
            ObservableRegistry.Register(so);

            var go = new GameObject();
            var sys = go.AddComponent<InvalidSystem>();
            sys.enabled = true;

            // Modify SO field
            so.number.Value = 99;

            // System should not throw, crash, or react
            // You could assert a side-effect if any

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}
