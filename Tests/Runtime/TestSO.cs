using ReaCS.Runtime;

namespace ReaCS.Tests.Runtime
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
            base.OnEnable();
        }
    }
}
