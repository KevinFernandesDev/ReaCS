using ReaCS.Runtime;
using ReaCS.Runtime.Internal;

namespace ReaCS.Tests.Shared
{
    [ReactTo(nameof(TestSO.number))]
    public class CustomFilterSystem : SystemBase<TestSO>
    {
        public int ChangeCount = 0;

        protected override void OnFieldChanged(TestSO changedSO)
        {
            ChangeCount++;
        }

        protected override bool IsTarget(TestSO so) => so.name == "Allow";
    }
}