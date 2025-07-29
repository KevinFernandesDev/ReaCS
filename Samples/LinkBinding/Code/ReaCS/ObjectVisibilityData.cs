using ReaCS.Runtime.Core;
using UnityEngine;

public class ObjectVisibilityData : ObservableScriptableObject
{
    [Observable] public Observable<bool> isVisible = new();
}