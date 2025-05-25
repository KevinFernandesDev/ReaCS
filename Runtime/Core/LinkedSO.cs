namespace ReaCS.Runtime.Core
{
    public class LinkedSO<TLeft, TRight> : ObservableScriptableObject
        where TLeft : ObservableScriptableObject
        where TRight : ObservableScriptableObject
    {
        public Observable<TLeft> Left = new();
        public Observable<TRight> Right = new();
    }
}