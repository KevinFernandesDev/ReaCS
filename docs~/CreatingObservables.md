# Creating Observables

To make a field observable, mark it with `[Observable]` and use the `Observable<T>` wrapper:

```csharp
[Observable] public Observable<int> playerScore;
```

Changes to `playerScore.Value` will trigger `OnChanged` events and dirty-checking.