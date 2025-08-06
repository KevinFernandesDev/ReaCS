using ReaCS.Runtime;
using ReaCS.Runtime.Core;
using UnityEngine;

namespace ReaCS.Tests.Shared
{
    public static class TestUtils
    {
        public static ObservableObject CreateDummySO(string name = "DummySO")
        {
            var so = ScriptableObject.CreateInstance<DummySO>();
            so.name = name;
            return so;
        }

        private class DummySO : Data
        {
            [Observable] public Observable<int> number;

            public override void OnEnable()
            {
                base.OnEnable();
                number.Init(this, nameof(number));
            }
        }
    }
}
