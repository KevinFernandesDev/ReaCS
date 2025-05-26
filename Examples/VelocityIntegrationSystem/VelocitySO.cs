using ReaCS.Runtime.Core;
using Unity.Mathematics;

namespace ReaCS.Examples
{
    public class VelocitySO : ObservableScriptableObject
    {
        [Observable] public Observable<float2> Value;
    }
}