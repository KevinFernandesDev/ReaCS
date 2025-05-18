using NUnit.Framework;
using UnityEngine;

public class ObservableScriptableObjectTests
{
    private class TestSO : ObservableScriptableObject
    {
        [Observable] public Observable<int> number;
        [Observable] public Observable<string> text;

        protected override void OnEnable()
        {
            base.OnEnable();
            number.Init(this, nameof(number));
            text.Init(this, nameof(text));
        }
    }

    private TestSO testSO;

    [Test]
    public void SO_Level_OnChanged_Triggers()
    {
        bool called = false;
        testSO.OnChanged += (so, field) => { if (field == nameof(testSO.number)) called = true; };
        testSO.number.Value = 10;
        testSO.CheckForChanges();
        Assert.IsTrue(called);
    }

    [Test]
    public void No_Change_Does_Not_Trigger()
    {
        testSO.text.Value = "Hello";
        testSO.CheckForChanges();

        bool called = false;
        testSO.OnChanged += (_, __) => called = true;
        testSO.CheckForChanges();
        Assert.IsFalse(called);
    }
}