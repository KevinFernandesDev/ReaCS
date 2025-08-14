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

    public interface IHasFastHash
    {
        /// <summary>
        /// Returns the current fast hash value without modifying internal state.
        /// </summary>
        int FastHashValue { get; }
    }

    public interface IObservableObjectFactory
    {
        T Create<T>(string name = null, EntityId? entityId = null) where T : ObservableObject;

        /// <summary>
        /// Deletes the saved JSON state for this instance (if any).
        /// </summary>
        bool DeleteState(ObservableObject instance, bool log = true);

        /// <summary>
        /// Destroys the runtime instance. If deleteState = true, also deletes its JSON snapshot.
        /// Won't write a final save on destroy.
        /// </summary>
        void Destroy(ObservableObject instance, bool deleteState = true, bool log = true);

        /// <summary>
        /// Utility: purge all snapshot files on disk (persistentDataPath + Temp in Editor).
        /// Returns the number of files deleted.
        /// </summary>
        int PurgeAllSnapshots(bool log = true);
    }

}
