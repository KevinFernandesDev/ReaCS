using UnityEngine;
using ReaCS.Runtime;
using ReaCS.Runtime.Internal;

namespace ReaCS.Tests.Shared
{
    [ReactTo(nameof(TestSO.number))]
    public class TestSystem : SystemBase<TestSO>
    {
        public TestSO lastChangedSO;
        public int callbackCount;

        protected override void OnFieldChanged(TestSO changedSO)
        {
            lastChangedSO = changedSO;
            callbackCount++;
        }
    }
}
