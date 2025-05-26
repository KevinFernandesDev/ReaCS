using ReaCS.Runtime.Core;
using Unity.Mathematics;

namespace ReaCS.Examples
{
    public class PositionSO : ObservableScriptableObject
    {
        [Observable] public Observable<float2> Value;
    }
}