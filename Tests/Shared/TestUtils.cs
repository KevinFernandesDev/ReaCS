using ReaCS.Runtime;
using ReaCS.Runtime.Core;
using UnityEngine;

namespace ReaCS.Tests.Shared
{
    public static class TestUtils
    {
        public static ObservableScriptableObject CreateDummySO(string name = "DummySO")
        {
            var so = ScriptableObject.CreateInstance<DummySO>();
            so.name = name;
            return so;
        }

        private class DummySO : ObservableScriptableObject
        {
            [Observable] public Observable<int> number;

            protected override void OnEnable()
            {
                base.OnEnable();
                number.Init(this, nameof(number));
            }
        }
    }
}
