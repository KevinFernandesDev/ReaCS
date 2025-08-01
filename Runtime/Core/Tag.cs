using ReaCS.Runtime;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    public class Tag : ObservableObject
    {
        public override void RegisterSelf()
        {
            Access.Query<TagRegistry>().Register(this);
        }
        public override void UnregisterSelf()
        {
            Access.Query<TagRegistry>().Unregister(this);
        }
    }
}
