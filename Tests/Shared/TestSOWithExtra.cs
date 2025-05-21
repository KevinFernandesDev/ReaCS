using ReaCS.Runtime;
using ReaCS.Runtime.Internal;

namespace ReaCS.Tests.Shared
{
    public class TestSOWithExtra : ObservableScriptableObject
    {
        [Observable] public Observable<int> number;
        public int hiddenField = 999;

        protected override void OnEnable()
        {
            base.OnEnable();
            number.Init(this, nameof(number));
        }

        public void ForceInitializeForTest()
        {
            base.OnEnable();
        }
    }
}