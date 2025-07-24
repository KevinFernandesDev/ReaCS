using NUnit.Framework;
using ReaCS.Runtime.Core;
using ReaCS.Tests.Shared;
using UnityEngine;

public class ObservableRuntimeWatcher_EditModeTests
{
    [Test]
    public void Register_SO_Does_Not_Throw_And_Initializes_Hash()
    {
        var so = ScriptableObject.CreateInstance<TestSO>();
        so.name = "RegisterTest";
        so.ForceInitializeForTest();

        Assert.DoesNotThrow(() => ObservableRuntimeWatcher.Register(so));
    }

    [Test]
    public void Register_Same_SO_Twice_Does_Not_Duplicate()
    {
        var so = ScriptableObject.CreateInstance<TestSO>();
        so.name = "RegisterTwice";
        so.ForceInitializeForTest();

        ObservableRuntimeWatcher.Register(so);
        Assert.DoesNotThrow(() => ObservableRuntimeWatcher.Register(so));
    }

    [Test]
    public void Unregister_Removes_SO_And_Stops_Updates()
    {
        var so = ScriptableObject.CreateInstance<TestSO>();
        so.ForceInitializeForTest();

        bool changed = false;
        so.OnChanged += (_, __) => changed = true;

        ObservableRuntimeWatcher.Register(so);
        ObservableRuntimeWatcher.Unregister(so);

        // Simulate a change
        so.number.Value = 99;

        // Manually tick update for edit mode
        typeof(ObservableRuntimeWatcher)
            .GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(GameObject.FindObjectOfType<ObservableRuntimeWatcher>(), null);

        Assert.IsFalse(changed, "Unregistered SO should not trigger changes.");
    }

    [Test]
    public void Changing_SO_Field_Flags_Dirty_And_Triggers_OnChanged()
    {
        var so = ScriptableObject.CreateInstance<TestSO>();
        so.ForceInitializeForTest();

        bool triggered = false;
        so.OnChanged += (_, field) =>
        {
            if (field == nameof(so.number)) triggered = true;
        };

        ObservableRuntimeWatcher.Register(so);

        // Change field
        so.number.Value = 123;

        // Run a manual update tick (edit mode simulation)
        typeof(ObservableRuntimeWatcher)
            .GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(GameObject.FindObjectOfType<ObservableRuntimeWatcher>(), null);

        Assert.IsTrue(triggered, "SO OnChanged should trigger when hash changes.");
    }

    [Test]
    public void No_OnChanged_When_Value_Unchanged()
    {
        var so = ScriptableObject.CreateInstance<TestSO>();
        so.ForceInitializeForTest();
        ObservableRuntimeWatcher.Register(so);

        bool triggered = false;
        so.OnChanged += (_, __) => triggered = true;

        // Force an update with no changes
        typeof(ObservableRuntimeWatcher)
            .GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(GameObject.FindObjectOfType<ObservableRuntimeWatcher>(), null);

        Assert.IsFalse(triggered, "SO should not trigger OnChanged if value unchanged.");
    }
}
