# Systems

A 'System' is any `MonoBehaviour` that reacts to changes in ObservableScriptableObjects.

```csharp
void OnEnable() {
    mySO.playerScore.OnChanged += val => Debug.Log("Score: " + val);
}
```

You can also use `OnChanged(so, fieldName)` on the SO to react to any change.