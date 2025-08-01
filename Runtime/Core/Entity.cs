using static ReaCS.Runtime.Access;
using UnityEngine;
using ReaCS.Runtime.Services;
using Unity.Collections;
using UnityEngine.SceneManagement;

namespace ReaCS.Runtime.Core
{
    public class Entity : MonoBehaviour
    {
        [HideInInspector]
        public EntityId entityId;

        private void Awake()
        {
            entityId = Access.Use<EntityService>().CreateEntityFor(transform);
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            if (entityId != default)
                Access.Use<EntityService>().Release(entityId);
        }

        private void OnDestroy()
        {
            if (entityId != default)
                Access.Use<EntityService>().Release(entityId);
        }


    }


}