# Getting Started

To begin using ReaCS:

1. Install the package via Unity Package Manager using Git:

```json
"com.yourcompany.reacs": "https://github.com/YOUR_USERNAME/reacs.git"
```

2. Create a new `ScriptableObject` that inherits from `ObservableScriptableObject`.

3. Use `[Observable] public Observable<T> myField;` to mark data fields.

4. Use `ObservableRuntimeWatcher` to automatically scan for changes at runtime.