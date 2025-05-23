namespace ReaCS.Runtime
{
    public interface IInitializableObservable
    {
        void Init(ObservableScriptableObject owner, string fieldName);
    }
}
