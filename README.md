# ReaCS - Reactive Component System for Unity

ReaCS is an opinionated lightweight Reactive Data-Driven Architecture that uses Observable data with ScriptableObjects to enable automatic UI binding, reactivity, and clean separation of data and logic with a state-as-truth behavior in Unity's game engine.

## Features
✅ State-as-truth

✅ Guardrails by design

✅ Close to zero boilerplate

✅ Transparent zero setup

✅ No inheritance promoted (Reactive SO data-driven architecture) compatible with interfaces when needed

✅ Promotes Interfaces for observable ScriptableObjects when needed

✅ Observables in ScriptableObjects baked in transparently behind the scenes

✅ Enforced SRP (Single-Responsability Principal) with "Systems" 

✅ Enforces *only one SO* to react to

✅ Enforces *only one field* to track

✅ Centralized runtime watcher with debounce for performance

✅ Clean API for devs: no subscriptions, no events, cross-monobehavior referencing or data/method accesses, and no string mistakes

✅ Works for designers using Unity regular workflow for data editing and addition of new data

✅ Easy to scale

✅ Easy to test

✅ Easy to debug (one system = one job)

✅ Easy to analyze, visualize, or extend later

✅ Easy to maintain

✅ Consistent architecture

✅ Covered by Unity Tests & Coverage tool (dependencies)

✅ Custom drawer for SO fields Inspector usability

✅ Custom graph-based debugging tool to inspect Systems, with

## 🔎 Code Coverage
[![Alt text](https://github.com/KevinFernandesDev/ReaCS/blob/main/badge_linecoverage.png)](https://github.com/KevinFernandesDev/ReaCS/blob/main/badge_linecoverage.png)

## Usage
1. Add the package via Git in your Unity project:
```json
"com.kevinfernandes.reacs": "https://github.com/KevinFernandesDev/ReaCS.git"
```

2. Create a ScriptableObject that extends `ObservableScriptableObject` and add Attribute `[Observable]` and `Observable<T>` to a field:
```csharp
public class ExperienceSO : ObservableScriptableObject {
    [Observable] public Observable<string> name;
    [Observable] public Observable<Sprite> icon;
}
```

3. In a Monobehavior inheriting from SystemBase, observe changes like this:
```csharp
[ReactTo(nameof(ExperienceSO.isSelected))]
public class ExperienceSelectSystem : SystemBase<ExperienceSO>
{
    protected override void OnFieldChanged(ExperienceSO changedSO)
    {
            // Reacts only when any ExperienceSO's `isSelected` changes
            experience.name.OnChanged += newName => Debug.Log("New name: " + newName);
    }
}
```

## 📘 Documentation
[![Docs](https://img.shields.io/badge/docs-online-blue)](https://github.com/KevinFernandesDev/ReaCS/wiki)

## License
No License
