using ReaCS.Runtime.Core;

namespace ReaCS.Tests.Shared
{
    [ReactTo(nameof(TestSO.number))]
    public class TestSystem : Reactor<TestSO>
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
