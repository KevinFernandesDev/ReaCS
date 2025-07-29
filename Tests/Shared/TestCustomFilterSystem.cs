using ReaCS.Runtime.Core;

namespace ReaCS.Tests.Shared
{
    [ReactTo(nameof(TestSO.number))]
    public class CustomFilterSystem : Reactor<TestSO>
    {
        public int ChangeCount = 0;

        protected override void OnFieldChanged(TestSO changedSO)
        {
            ChangeCount++;
        }

        protected override bool IsTarget(TestSO so) => so.name == "Allow";
    }
}