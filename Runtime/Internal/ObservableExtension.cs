using ReaCS.Runtime.Core;

namespace ReaCS.Runtime.Internal
{
    public static class ObservableExtensions
    {
        public static T Get<T>(this Observable<T> observable) => observable.Value;
        public static void Set<T>(this Observable<T> observable, T value) => observable.Value = value;
    }
}
