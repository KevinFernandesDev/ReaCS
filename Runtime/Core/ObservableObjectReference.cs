using System;

namespace ReaCS.Runtime.Core
{
    /// <summary>
    /// ObservableSO<T> exists for a very specific architectural reason, asit lets us model references to other ObservableObjects in a way that is:
    /// Type-safe: Only allows linking to other data objects in your ReaCS system (not arbitrary Unity assets).
    /// Observable: Supports reactive patterns, so we can detect when a reference to another SO changes, and propagate reactions or signals accordingly.
    /// Enforces constraints: Only references types derived from ObservableObject, enforcing the architecture you want for links.
    /// 
    /// It's important especially for links as using Observable<T> would actually serialize/clone the object instead of it being just a pointer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class ObservableObjectReference<T> : Observable<T>, IObservableReference where T : ObservableObject
    {
        ObservableObject IObservableReference.Value => Value;
    }
}