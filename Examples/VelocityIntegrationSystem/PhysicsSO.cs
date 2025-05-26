using ReaCS.Runtime.Core;
using UnityEngine;

public class PhysicsSO : ObservableScriptableObject
{
    [Observable] public Observable<Vector3> velocity;
}
