namespace ReaCS.Runtime.Core
{
    public class LinkedSO<TLeft, TRight> : ObservableScriptableObject
        where TLeft : ObservableScriptableObject
        where TRight : ObservableScriptableObject
    {
        public ObservableSO<TLeft> Left = new();
        public ObservableSO<TRight> Right = new();
    }
}