using System;
using ReaCS.Runtime;
using ReaCS.Runtime.Core;

namespace ReaCS.Tests.Shared
{
    public class DebounceTestSO : Data
    {
        [Observable] public Observable<int> number;

        public event Action OnTestChanged;
        public int CheckCount { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            number.Init(this, nameof(number));
            OnChanged += (_, __) => OnTestChanged?.Invoke(); // bridge to test logic
        }

        public void ForceInitializeForTest()
        {
            CheckCount = 0;
            OnTestChanged = null;
            OnEnable(); // ensure observables are linked and event is hooked
        }

        // Called manually in test to simulate SO becoming dirty
        public void TriggerChange(int newValue)
        {
            number.Value = newValue;
            MarkDirty(number.ToString());
        }

        // Optional method for debugging test flow
        public void IncrementCheckCount()
        {
            CheckCount++;
        }
    }
}
