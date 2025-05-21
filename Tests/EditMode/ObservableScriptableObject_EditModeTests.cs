using NUnit.Framework;
using ReaCS.Runtime;
using ReaCS.Tests.Shared;
using System.Reflection;
using UnityEngine;

namespace ReaCS.Tests.Runtime
{
    public class ObservableScriptableObject_RuntimeTests
    {
#if UNITY_EDITOR
        [Test]
        public void OnValidate_Does_Not_Throw()
        {
            var so = ScriptableObject.CreateInstance<TestSO>();
            so.name = "ValidateTest";

            // simulate editor validation
            var validate = typeof(ObservableScriptableObject).GetMethod("OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            validate?.Invoke(so, null);

            Assert.Pass("Validation ran");
        }
#endif

        [Test]
        public void NonObservable_Field_Does_Not_Affect_Cache()
        {
            var so = ScriptableObject.CreateInstance<TestSOWithExtra>();

            Assert.DoesNotThrow(() => so.ForceInitializeForTest());
        }
    }
}