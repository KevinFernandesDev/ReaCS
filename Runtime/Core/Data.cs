using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using ReaCS.Runtime.Services;

namespace ReaCS.Runtime.Core
{
    public class Data : ObservableObject
    {
        public override void RegisterSelf()
        {
            Access.Query<DataRegistry>().Register(this);
        }

        public override void UnregisterSelf()
        {
            Access.Query<DataRegistry>().Unregister(this);
        }
    }
}