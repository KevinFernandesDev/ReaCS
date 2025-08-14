using ReaCS.Runtime.Core;
using UnityEngine;
using EntityId = ReaCS.Runtime.Core.EntityId;

namespace ReaCS.Runtime.Services
{
    public class ObservableObjectFactory : IObservableObjectFactory, IReaCSService
    {
        public T Create<T>(string name = null, EntityId? entityId = null) where T : ObservableObject
        {
            using (new ObservableObject.NameInjectionScope(name ?? typeof(T).Name))
            {
                var instance = ScriptableObject.CreateInstance<T>();

                if (entityId.HasValue)
                    instance.entityId = entityId.Value;

                return instance;
            }
        }
    }
}
