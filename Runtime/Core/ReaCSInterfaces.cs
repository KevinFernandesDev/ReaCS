namespace ReaCS.Runtime.Core
{
    public interface IReaCSService { }
    public interface IReaCSQuery { }

    public interface IHasOwner<TOwner> where TOwner : ObservableScriptableObject
    {
        Observable<TOwner> Owner { get; }
    }

    public interface IHasData<TData> where TData : ObservableScriptableObject
    {
        TData data { get; }
    }

    public interface IInitializableObservable
    {
        void Init(ObservableScriptableObject owner, string fieldName);
    }
}
