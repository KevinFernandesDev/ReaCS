using UnityEngine;

namespace ReaCS.Runtime.Core
{
    public interface IReaCSService { }
    public interface IReaCSQuery { }
    public interface IObservableReference
    {
        ObservableScriptableObject Value { get; }
    }

    public interface IHasOwner<TOwner> where TOwner : ObservableScriptableObject
    {
        Observable<TOwner> Owner { get; }
    }

    public interface IHasDataSource<T> where T : ObservableScriptableObject
    {
        T DataSource { get; }
        bool UseAsTemplate { get; }
    }

    public interface IHasUnityComponent<T> where T : Component
    {
        T UnityComponent { get; set; }
    }

    public interface IHasEntityId
    {
        Observable<int> entityId { get; }
    }

    public interface IInitializableObservable
    {
        void Init(ObservableScriptableObject owner, string fieldName);
    }
}
