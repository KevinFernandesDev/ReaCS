using ReaCS.Runtime.Core;

namespace ReaCS.Examples
{
    public class Example_HealthSO : ObservableScriptableObject
    {
        [Observable] public Observable<int> value;
        [Observable] public Observable<bool> isCritical;
    }
}