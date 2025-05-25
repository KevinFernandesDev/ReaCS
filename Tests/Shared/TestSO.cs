using ReaCS.Runtime;
using ReaCS.Runtime.Core;

namespace ReaCS.Tests.Shared
{
    public class TestSO : ObservableScriptableObject
    {
        [Observable] public Observable<int> number;

        protected override void OnEnable()
        {
            base.OnEnable();
            number.Init(this, nameof(number));
        }

        public void ForceInitializeForTest()
        {
            OnEnable(); // force hook-up if needed
        }
    }
}