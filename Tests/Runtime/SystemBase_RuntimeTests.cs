using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using ReaCS.Runtime;
using ReaCS.Runtime.Internal;
using ReaCS.Tests.Runtime;
using ReaCS.Tests.Shared;

namespace ReaCS.Tests
{
    public class SystemBase_RuntimeTests
    {
        [UnitySetUp]
        public IEnumerator EnsureUnityReady()
        {
            yield return new WaitForSeconds(0.01f);
        }

        [UnityTest]
        public IEnumerator System_Reacts_When_Field_Changes()
        {
            ObservableRegistry.ClearAll();

            var testSO = ScriptableObject.CreateInstance<TestSO>();
            testSO.number.Init(testSO, nameof(testSO.number));
            ObservableRegistry.Register(testSO);

            yield return new WaitForSeconds(0.01f);

            var go = new GameObject("TestSystem");
            var sys = go.AddComponent<TestSystem>();
            sys.enabled = true;

            yield return new WaitForSeconds(0.01f);

            testSO.number.Value = 42;
            yield return new WaitForSeconds(0.01f);

            Assert.AreEqual(testSO, sys.lastChangedSO);
            Assert.AreEqual(1, sys.callbackCount);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(testSO);
        }

        [UnityTest]
        public IEnumerator OnEnable_RegistersCorrectly()
        {
            ObservableRegistry.ClearAll();

            var so = ScriptableObject.CreateInstance<TestSO>();
            so.number.Init(so, nameof(so.number));
            ObservableRegistry.Register(so);

            yield return new WaitForSeconds(0.01f);

            var go = new GameObject("Sys");
            var sys = go.AddComponent<TestSystem>();
            sys.enabled = true;

            yield return new WaitForSeconds(0.01f);

            so.number.Value = 99;
            yield return new WaitForSeconds(0.01f);

            Assert.AreEqual(so, sys.lastChangedSO);
            Assert.AreEqual(1, sys.callbackCount);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [UnityTest]
        public IEnumerator OnDisable_UnsubscribesCorrectly()
        {
            ObservableRegistry.ClearAll();

            var so = ScriptableObject.CreateInstance<TestSO>();
            so.number.Init(so, nameof(so.number));
            ObservableRegistry.Register(so);

            yield return new WaitForSeconds(0.01f);

            var go = new GameObject("Sys");
            var sys = go.AddComponent<TestSystem>();
            sys.enabled = true;

            yield return new WaitForSeconds(0.01f);

            sys.enabled = false;

            so.number.Value = 123;
            yield return new WaitForSeconds(0.01f);

            Assert.IsNull(sys.lastChangedSO);
            Assert.AreEqual(0, sys.callbackCount);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [UnityTest]
        public IEnumerator SystemBase_React_To_Matched_SO()
        {
            ObservableRegistry.ClearAll();

            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "Allow";
            so.number.Init(so, nameof(so.number));
            ObservableRegistry.Register(so);

            yield return new WaitForSeconds(0.01f);

            var go = new GameObject("FilterSystem");
            var sys = go.AddComponent<CustomFilterSystem>();
            sys.enabled = true;

            yield return new WaitForSeconds(0.01f);

            so.number.Value++;
            yield return new WaitForSeconds(0.01f);

            Assert.AreEqual(1, sys.ChangeCount);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [UnityTest]
        public IEnumerator SystemBase_Ignores_Unmatched_SO()
        {
            ObservableRegistry.ClearAll();

            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "Deny";
            so.number.Init(so, nameof(so.number));
            ObservableRegistry.Register(so);

            yield return new WaitForSeconds(0.01f);

            var go = new GameObject("FilterSystem");
            var sys = go.AddComponent<CustomFilterSystem>();
            sys.enabled = true;

            yield return new WaitForSeconds(0.01f);

            so.number.Value++;
            yield return new WaitForSeconds(0.01f);

            Assert.AreEqual(0, sys.ChangeCount);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [UnityTest]
        public IEnumerator Reacts_During_PlayLifecycle()
        {
            ObservableRegistry.ClearAll();

            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "Allow";
            so.number.Init(so, nameof(so.number));
            ObservableRegistry.Register(so);

            yield return new WaitForSeconds(0.01f);

            var go = new GameObject("FilterSystem");
            var sys = go.AddComponent<CustomFilterSystem>();
            sys.enabled = true;

            yield return new WaitForSeconds(0.01f);

            so.number.Value++;
            yield return new WaitForSeconds(0.01f);

            Assert.AreEqual(1, sys.ChangeCount);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}
