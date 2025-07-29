using ReaCS.Runtime.Core;

namespace ReaCS.Tests.Shared
{
    public class InvalidSystem : Reactor<TestSO>
    {
        protected override void OnFieldChanged(TestSO changedSO) { }
    }
}