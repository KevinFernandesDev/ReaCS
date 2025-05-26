using ReaCS.Runtime.Core;

namespace ReaCS.Examples
{
    /// <summary>
    /// This PositionDataComponent binds an ObservableScriptableObject (e.g PositionSO) 
    /// that will be created at runtime to this specific object in scene.
    /// The purpose is to be able to sync up automatically an ObservableScriptableObject using ComponentDataBinding<TSO>.
    /// An instance of it will be created and pooled at runtime automatically and bound to the 'data' field.
    /// This instance of the ObservableScriptableObject can then be used by systems to update any kind of unity component.
    /// </summary>
    public class PositionDataComponent : ComponentDataBinding<PositionSO> { }
}
