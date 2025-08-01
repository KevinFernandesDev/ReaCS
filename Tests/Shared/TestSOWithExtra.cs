using ReaCS.Runtime;
using ReaCS.Runtime.Core;

namespace ReaCS.Tests.Shared
{
    public class TestSOWithExtra : Data
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