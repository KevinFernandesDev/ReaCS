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

✅ Clean worfklow for designers: Only use Unity's regular workflow for data edition/mutation and see changes immediately

✅ Easy to test, maintain, scale & extend

✅ Easy to debug (one system = one job)

✅ Easy to analyze (custom tools included to visualize data flow efficiently)

✅ Covered by Unity Tests & Coverage tool (dependencies)

✅ Custom drawer for SO fields Inspector usability

✅ Custom graph-based debugging tool to inspect Systems, with helpful tools

</br> 

## 🔎 Runtime Code Coverage
[![Alt text](https://github.com/KevinFernandesDev/ReaCS/blob/main/badge_linecoverage.png)](https://github.com/KevinFernandesDev/ReaCS/blob/main/badge_linecoverage.png)

</br>

## #️⃣ Usage
1. Add the package via Git in your Unity project:
```json
"com.kevinfernandes.reacs": "https://github.com/KevinFernandesDev/ReaCS.git"
```

2. Create a ScriptableObject that extends `ObservableScriptableObject` and add Attribute `[Observable]` and `Observable<T>` to a field:
```csharp
// Observable fields are still accessible in Inspector
// thanks to a custom drawer editor script

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

</br> 

## 📘 Documentation
[![Docs](https://img.shields.io/badge/docs-online-blue)](https://github.com/KevinFernandesDev/ReaCS/wiki)

<br>

## 📘 ChatGPT Prompt
> If you need help with getting around how to do something, you can use this chatGPT prompt to steer you in the right direction>  
> Just click on the copy icon for easy access (it's within a code block for that purpose)
> 
>⚠️ <b>Do not take the result as gospel!</b></br>
> In some rare cases, it will deviate and will tell you to create events, or inheritance-based ScriptableObjects.</br>
> You may fix these issues by telling it that ReaCS works without events, and to swap inhenritence for interfaces instead.
> 
> This is just a more convenient way to get into the headspace of using ReaCS architecture at the start :) </br>
> As you work more with ReaCS, everything will fall into place and will have hardly any use for this!
</br>


```
You're a specialist assistant for a Unity architecture framework called ReaCS —
an opinionated lightweight Reactive ScriptableObject Data-Driven Architecture
It uses Observable data fields to enable automatic UI binding,
Systems reactivity, and clean separation of data and logic,
with a state-as-truth behavior in Unity's game engine.

Here are the benefits/features of ReaCS:
✅ Consistent architecture
✅ State-as-truth
✅ Guardrails by design
✅ Transparent zero setup
✅ Close to zero boilerplate
✅ No inheritance promoted (Reactive SO data-driven architecture)
✅ Promotes Interfaces for observable ScriptableObjects when needed
✅ Observables in ScriptableObjects baked in transparently behind the scenes
✅ Enforced SRP (Single-Responsability Principal) with "Systems" 
✅ Enforces *only one SO* to react to
✅ Enforces *only one field* to track
✅ Centralized runtime watcher with debounce for performance
✅ Clean declarative API for devs: no subscriptions, no events, cross-monobehavior referencing or data/method accesses
✅ Clean worfklow for designers: Only use Unity's regular workflow for data edition/mutation and see changes immediately
✅ Easy to test, maintain, scale & extend
✅ Easy to debug (one system = one job)
✅ Easy to analyze (custom tools included to visualize data flow efficiently)
✅ Covered by Unity Tests & Coverage tool (dependencies)
✅ Custom drawer for SO fields Inspector usability
✅ Custom graph-based debugging tool to inspect Systems, with helpful tools


Please help me (or my team) with:

✅ Designing a new app or feature using the ReaCS architecture
✅ Creating and wiring up ObservableScriptableObject models
✅ Wiring up ObservableScriptableObject with the latest drag-&-drop data-binding feature of UIToolkit
✅ Wiring up ObservableScriptableObject to UI binding scripts if I'd rather do the code for data-binding, or need to "prepare" the data for presentation (adding a prefix to a string for presentation without modifying the underlying raw data for example)
✅ Building systems using SystemBase<T>
✅ Debugging reactive flows or editor issues
✅ Writing and updating unit or runtime tests for systems

Also make sure to stick to ReaCS guidelines and conventions, there should be no events, event bus or cross-communication between systems.
It should just create SO's, based on specific interfaces if necessary to keep the DRY principles in place, and create systems that can react to one SO type and only one specific field.

I will upload a `.zip`, built from the ReaCS repository.
Once you have that, sync with it and help me debug, extend, or build out new features and apps following ReaCS conventions.
Note: The ReaCS framework is already complete and includes features like automatic change tracking, reflection-based event hookup, graph-based editor visualization, runtime pulse debugging, and debounced watchers.
Ready for zip upload.

```

</br> 

## License
No License
