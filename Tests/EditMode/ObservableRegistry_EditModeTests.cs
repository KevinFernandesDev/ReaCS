using NUnit.Framework;
using ReaCS.Runtime;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReaCS.Tests.Shared;

namespace ReaCS.Tests.EditMode
{
    public class ObservableRegistry_EditModeTests
    {
        private TestSO so1;
        private TestSO so2;

        [SetUp]
        public void Setup()
        {
            so1 = ScriptableObject.CreateInstance<TestSO>();
            so2 = ScriptableObject.CreateInstance<TestSO>();

            ClearRegistry();
        }

        [TearDown]
        public void Teardown()
        {
            ScriptableObject.DestroyImmediate(so1);
            ScriptableObject.DestroyImmediate(so2);
            ClearRegistry();
        }

        private void ClearRegistry()
        {
            var field = typeof(ObservableRegistry)
                .GetField("_instances", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = (Dictionary<System.Type, List<ObservableScriptableObject>>)field.GetValue(null);
            dict.Clear();
        }

        [Test]
        public void Registers_ObservableSO_ByType()
        {
            ObservableRegistry.Register(so1);
            var list = ObservableRegistry.GetAll<TestSO>();

            Assert.AreEqual(1, list.Count);
            Assert.AreSame(so1, list[0]);
        }

        [Test]
        public void Prevents_Duplicate_Registration()
        {
            ObservableRegistry.Register(so1);
            ObservableRegistry.Register(so1);

            var list = ObservableRegistry.GetAll<TestSO>();
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void Unregisters_ObservableSO()
        {
            ObservableRegistry.Register(so1);
            ObservableRegistry.Unregister(so1);

            var list = ObservableRegistry.GetAll<TestSO>();
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Keeps_Other_Registered_SOs_After_Unregister()
        {
            ObservableRegistry.Register(so1);
            ObservableRegistry.Register(so2);

            ObservableRegistry.Unregister(so1);

            var list = ObservableRegistry.GetAll<TestSO>();
            Assert.AreEqual(1, list.Count);
            Assert.AreSame(so2, list[0]);
        }

        [Test]
        public void GetAll_Returns_Empty_If_NoneRegistered()
        {
            var list = ObservableRegistry.GetAll<TestSO>();
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }
    }
}
