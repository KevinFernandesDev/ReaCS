using UnityEngine;

namespace ReaCS.Runtime.Core
{
    public interface IReaCSService { }
    public interface IReaCSQuery { }
    public interface IObservableReference
    {
        ObservableObject Value { get; }
    }

    public interface IHasOwner<TOwner> where TOwner : ObservableObject
    {
        Observable<TOwner> Owner { get; }
    }

    public interface IHasDataSource<T> where T : ObservableObject
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
        void Init(ObservableObject owner, string fieldName);
    }
    public interface ILinkResettable
    {
        void ClearLink();
    }
    public interface IPool
    {
        void Release(IPoolable obj);
    }

    public interface IPoolable
    {
        void SetPool(IPool pool);
        void Initialize();
        void Release();
    }

    public interface ICoreRegistrable
    {
        void Register();
        void Unregister();
    }

    public interface IRegistrable
    {
        void RegisterSelf();
        void UnregisterSelf();
    }
    public interface ILinkConnector
    {
        void Connect(ObservableObject left, ObservableObject right);
    }
}
