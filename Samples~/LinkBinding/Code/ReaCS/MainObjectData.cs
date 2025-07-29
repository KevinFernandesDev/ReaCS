using ReaCS.Runtime.Core;
using UnityEngine;

public class MainObjectData : ObservableScriptableObject
{
    [Observable] public Observable<bool> isVisible = new();
}
