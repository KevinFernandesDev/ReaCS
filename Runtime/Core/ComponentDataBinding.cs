using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Registries;
using ReaCS.Runtime.Services;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    /// <summary>
    /// Runtime binding between a GameObject and its ObservableScriptableObject component.
    /// </summary>
    public abstract class ComponentDataBinding : MonoBehaviour
    {
        public abstract void InitializeData();
    }

    /// <summary>
    /// Generic binding to either a shared or pooled instance of TSO.
    /// A single field is used, and a toggle determines if it's used as a template or directly.
    /// </summary>
    public abstract class ComponentDataBinding<TSO> : ComponentDataBinding, IHasDataSource<TSO>
        where TSO : ObservableScriptableObject, new()
    {
        [SerializeField] private TSO dataSource;
        [SerializeField] private bool useAsTemplate = false;

        public TSO DataSource => dataSource;
        public bool UseAsTemplate => useAsTemplate;

        public TSO data { get; private set; }


        private bool initialized;

        public override void InitializeData()
        {
            if (initialized) return;

            int sharedId = Use<SharedEntityIdService>().GetOrAssignEntityId(transform);

            if (dataSource != null)
            {
                if (useAsTemplate)
                {
                    data = Use<PoolService<TSO>>().Get();
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(dataSource), data);
                    data.name = $"{dataSource.name}_Runtime_{GetInstanceID()}";
                }
                else
                {
                    data = dataSource;
                }
            }
            else
            {
                // Fallback: blank pooled instance
                data = Use<PoolService<TSO>>().Get();
                data.name = $"Unnamed_{typeof(TSO).Name}_Runtime_{GetInstanceID()}";
            }

            // Assign shared EntityId if supported
            if (data is IHasEntityId withId)
                withId.entityId.Value = sharedId;

            Use<ComponentDataBindingService<TSO>>().Register(data, this);
            Query<IndexRegistry>().Register(data);         

            initialized = true;
        }

        protected virtual void OnEnable() => InitializeData();

        protected virtual void OnDestroy()
        {
            if (data == null) return;

            Use<ComponentDataBindingService<TSO>>().Unregister(data);
            Query<IndexRegistry>().Unregister(data);

            if (data != dataSource || useAsTemplate || dataSource == null)
            {
                Use<PoolService<TSO>>().Release(data);
            }
        }
    }
}