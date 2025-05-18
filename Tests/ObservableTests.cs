using NUnit.Framework;
using UnityEngine;

public class ObservableTests
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
    public void Observable_Field_Level_OnChanged_Triggers()
    {
        bool called = false;
        testSO.number.OnChanged += val => { if (val == 42) called = true; };
        testSO.number.Value = 42;
        Assert.IsTrue(called);
    }

    [Test]
    public void Observable_Does_Not_Trigger_If_Value_Same()
    {
        testSO.number.Value = 10;
        bool called = false;
        testSO.number.OnChanged += val => called = true;
        testSO.number.Value = 10;
        Assert.IsFalse(called);
    }
}