using NUnit.Framework;
using UnityEngine;

public class ObservableRuntimeWatcherTests
{
    private class TestSO : ObservableScriptableObject
    {
        [Observable] public Observable<int> number;

        protected override void OnEnable()
        {
            base.OnEnable();
            number.Init(this, nameof(number));
        }
    }

    private TestSO testSO;

    [Test]
    public void Registers_And_Updates_SO()
    {
        bool triggered = false;
        testSO.OnChanged += (_, field) => {
            if (field == nameof(testSO.number)) triggered = true;
        };

        testSO.number.Value = 123;
        ObservableRuntimeWatcher.Register(testSO);
        ObservableRuntimeWatcher.ForceUpdate();

        Assert.IsTrue(triggered);
    }

    [Test]
    public void Debounce_Does_Not_Update_Immediately()
    {
        testSO.number.Value = 456;
        ObservableRuntimeWatcher.Register(testSO);
        ObservableRuntimeWatcher.DebounceChange(testSO, 1.0f);

        bool triggered = false;
        testSO.OnChanged += (_, __) => triggered = true;
        ObservableRuntimeWatcher.ForceUpdate();

        Assert.IsFalse(triggered);
    }
}