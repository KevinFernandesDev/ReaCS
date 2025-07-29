using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Registries;
using ReaCS.Runtime.Services;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    /// <summary>
    /// Runtime binding between a GameObject and its ObservableScriptableObject component and UnityComponent.
    /// </summary>
    public abstract class ComponentDataBinding : MonoBehaviour
    {
        public abstract void InitializeData();
    }

    /// <summary>
    /// Base class used internally by ComponentDataBindingService for data-only access.
    /// </summary>
    public abstract class ComponentDataBinding<TSO> : ComponentDataBinding
        where TSO : ObservableScriptableObject
    {
        public abstract TSO data { get; set; }
    }


    /// <summary>
    /// Generic binding to a data source of type TSO and a required Unity Component TUC.
    /// </summary>
    public abstract class ComponentDataBinding<TSO, TUC> : ComponentDataBinding<TSO>, IHasDataSource<TSO>
        where TSO : ObservableScriptableObject, new()
        where TUC : Component
    {
        [SerializeField] private TSO dataSource;
        [SerializeField] private bool useAsTemplate = false;

        public TSO DataSource => dataSource;
        public bool UseAsTemplate => useAsTemplate;

        public override TSO data { get; set; }
        public TUC uc { get; private set; }

        private bool initialized;

        public override void InitializeData()
        {
            if (initialized) return;

            uc = GetComponent<TUC>();
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
                data = Use<PoolService<TSO>>().Get();
                data.name = $"Unnamed_{typeof(TSO).Name}_Runtime_{GetInstanceID()}";
            }

            if (data is IHasEntityId withId)
                withId.entityId.Value = sharedId;

            Query<ComponentDataBindingRegistry<TSO>>().Register(data, this);
            Query<IndexRegistry>().Register(data);

            initialized = true;
        }

        protected virtual void OnEnable() => InitializeData();

        protected virtual void OnDestroy()
        {
            if (data == null) return;

            Query<ComponentDataBindingRegistry<TSO>>().Unregister(data);
            Query<IndexRegistry>().Unregister(data);

            if (data != dataSource || useAsTemplate || dataSource == null)
            {
                Use<PoolService<TSO>>().Release(data);
            }
        }
    }
}
