# ReaCS - Reactive Component System for Unity

ReaCS is an opinionated lightweight <b><i>Reactive ScriptableObject Data-Driven Architecture</i></b> that uses Observable data fields to enable automatic UI binding, Systems reactivity, and clean separation of data and logic, with a state-as-truth behavior in Unity's game engine.

## Features
✅ Consistent architecture

✅ State-as-truth

✅ Guardrails by design

✅ Transparent zero setup

✅ Close to zero boilerplate (compared to INotifyPropertyChanged for example)

✅ No inheritance promoted (Reactive SO data-driven architecture)

✅ Promotes Interfaces for observable ScriptableObjects when needed

✅ Observables in ScriptableObjects baked in transparently behind the scenes

✅ Enforced SRP (Single-Responsability Principal) with "Systems" 

✅ Enforces *only one SO* to react to

✅ Enforces *only one field* to track

✅ Centralized runtime watcher with debounce for performance

✅ Clean declarative API for devs: no subscriptions, no events, cross-monobehavior referencing or data/method accesses

✅ Clean worfklow for for designers: Only use Unity's regular workflow for data edition/mutation and see changes immediately

✅ Easy to test, maintain, scale & extend

✅ Easy to debug (one system = one job)

✅ Easy to analyze (custom tools included to visualize data flow efficiently)

✅ Covered by Unity Tests & Coverage tool (dependencies)

✅ Custom drawer for SO fields Inspector usability

✅ Custom graph-based debugging tool to inspect Systems, with

<br> 

## 🔎 Code Coverage
[![Alt text](https://github.com/KevinFernandesDev/ReaCS/blob/main/badge_linecoverage.png)](https://github.com/KevinFernandesDev/ReaCS/blob/main/badge_linecoverage.png)

<br> 

## #️⃣ Usage
1. Add the package via Git in your Unity project:
```json
"com.kevinfernandes.reacs": "https://github.com/KevinFernandesDev/ReaCS.git"
```

2. Create a ScriptableObject that extends `ObservableScriptableObject` and add Attribute `[Observable]` and `Observable<T>` to a field:
```csharp
// Observable fields are still accessible in Inspector thanks to custom drawer editor script
public class ExperienceSO : ObservableScriptableObject {
    [Observable] public Observable<string> name;
    [Observable] public Observable<Sprite> icon;
}
```

3. In a Monobehavior inheriting from SystemBase, observe changes like this:
```csharp
// Select the ScriptableObject type and field you want to react to
// Behavior automatically runs in OnFieldChanged (automatically filled via abstract method in SystemBase)
[ReactTo(nameof(ExperienceSO.isSelected))]
public class ExperienceSelectionSystem : SystemBase<ExperienceSO>
{
    protected override void OnFieldChanged(ExperienceSO changedSO)
    {
            // Reacts only when any ExperienceSO's `isSelected` changes
            experience.name.OnChanged += newName => Debug.Log("New name: " + newName);
    }
}
```

<br> 

## 📘 Documentation
[![Docs](https://img.shields.io/badge/docs-online-blue)](https://github.com/KevinFernandesDev/ReaCS/wiki)

<br> 

## License
No License
