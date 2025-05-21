using ReaCS.Runtime;

namespace ReaCS.Tests.Shared
{
    public class InvalidSystem : SystemBase<TestSO>
    {
        protected override void OnFieldChanged(TestSO changedSO) { }
    }
}