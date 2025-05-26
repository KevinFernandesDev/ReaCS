using System;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class ObservableSO<T> : Observable<T>, IObservableReference where T : ObservableScriptableObject
    {
        ObservableScriptableObject IObservableReference.Value => Value;
    }
}