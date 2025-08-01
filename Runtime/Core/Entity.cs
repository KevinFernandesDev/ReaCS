using static ReaCS.Runtime.Access;
using UnityEngine;
using ReaCS.Runtime.Services;
using Unity.Collections;

namespace ReaCS.Runtime.Core
{
    public class Entity : MonoBehaviour
    {
        [HideInInspector]
        public EntityId entityId;

        private void Awake()
        {
            entityId = Access.Use<EntityService>().CreateEntityFor(transform);
        }

        private void OnDestroy()
        {
            if (entityId != default)
                Access.Use<EntityService>().Release(entityId);
        }
    }

}