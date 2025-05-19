# ReaCS - Reactive Component System for Unity

ReaCS is a lightweight Reactive ECS-inspired architecture that uses Observable data with ScriptableObjects to enable automatic UI binding, reactivity, and clean separation of data and logic.

## Features
- [x] Observable<T> with per-field OnChanged
- [x] ObservableScriptableObject with dirty tracking
- [x] Centralized runtime watcher with debounce for performance
- [x] Custom drawer for Inspector usability
- [x] Works with Unity's UI Toolkit and data binding
- [x] Supports two-way bindings via ScriptableObjects
- [x] Battle-tested for complex projects
- [x] Covered by Unity Tests & Coverage tool (dependencies)
      
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

3. In a MonoBehaviour, observe changes like this:
```csharp
experience.name.OnChanged += newName => Debug.Log("New name: " + newName);
```

## 📘 Documentation
[![Docs](https://img.shields.io/badge/docs-online-blue)](https://github.com/KevinFernandesDev/ReaCS/wiki)

## 🔎 Code Coverage
[![Alt text](https://github.com/KevinFernandesDev/ReaCS/blob/main/badge_linecoverage.png)](https://github.com/KevinFernandesDev/ReaCS/blob/main/badge_linecoverage.png)
## License
No License
