using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Registries;
using ReaCS.Runtime.Services;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    /// <summary>
    /// Runtime binding between a GameObject, a Data asset, and a UnityComponent.
    /// </summary>
    public abstract class DataAdapterComponent : MonoBehaviour
    {
        public abstract void InitializeData();
    }

    public abstract class DataAdapterBase<TData> : DataAdapterComponent
        where TData : Data
    {
        public abstract TData data { get; set; }
    }

    /// <summary>
    /// Binds a Data object to a Unity component, supports pooling and runtime Entity assignment.
    /// Only supports Data (never Tag/Link) as TData.
    /// </summary>
    public abstract class DataAdapter<TData, TUC> : DataAdapterBase<TData>, IHasDataSource<TData>
        where TData : Data, new()
        where TUC : Component
    {
        [SerializeField] private TData dataSource;
        [SerializeField] private bool useAsTemplate = false;

        public TData DataSource => dataSource;
        public bool UseAsTemplate => useAsTemplate;

        public override TData data { get; set; }
        public TUC component { get; private set; }

        // --- Runtime service setup fields ---
        private EntityId? _forcedEntityId = null;
        private TData _forcedDataSource = null;
        private bool _forcedUseAsTemplate = false;
        protected bool initialized = false;

        /// <summary>
        /// Called by DataAdapterService for runtime configuration.
        /// </summary>
        public void SetupForRuntime(EntityId? entityId, TData dataSource, bool useAsTemplate)
        {
            _forcedEntityId = entityId;
            _forcedDataSource = dataSource;
            _forcedUseAsTemplate = useAsTemplate;
        }

        public override void InitializeData()
        {
            if (initialized) return;

            component = GetComponent<TUC>();

            EntityId resolvedEntityId = _forcedEntityId ?? ResolveOrCreateEntity().entityId;
            TData source = _forcedDataSource ?? dataSource;
            bool template = _forcedUseAsTemplate || useAsTemplate;
            bool createRuntime = (source == null) || template;

            if (createRuntime)
            {
                data = Access.Use<DataPool<TData>>().Get(resolvedEntityId);
                if (template && source != null)
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), data);

                data.name = template && source != null
                    ? $"_Runtime_{source.name}_{GetInstanceID()}"
                    : $"Runtime_{typeof(TData).Name}_{GetInstanceID()}";
            }
            else
            {
                data = source;
            }

            data.entityId = resolvedEntityId;

            Query<DataAdapterRegistry<TData>>().Register(data, this);
            Query<IndexRegistry>().Register(data);

            initialized = true;
        }

        /// <summary>
        /// Finds an Entity in the hierarchy, adds one to the root if needed, and warns the user.
        /// </summary>
        protected Entity ResolveOrCreateEntity()
        {
            Entity entity = null;
            Transform cursor = transform;
            while (cursor != null && entity == null)
            {
                entity = cursor.GetComponent<Entity>();
                if (entity != null) break;
                cursor = cursor.parent;
            }

            if (entity == null)
            {
                var root = transform.root;
                entity = root.gameObject.AddComponent<Entity>();

                Debug.LogWarning(
                    $"[ReaCS] No Entity found in hierarchy for DataAdapter<{typeof(TData).Name},{typeof(TUC).Name}> on '{gameObject.name}'.\n" +
                    $"An Entity component has been automatically added to root '{root.name}'. " +
                    $"It's recommended to explicitly add Entity to your prefab or scene root for optimal performance.",
                    this
                );
            }

            if (entity.entityId == EntityId.None)
                entity.entityId = Access.Use<EntityService>().CreateEntityFor(entity.transform);

            return entity;
        }

        protected virtual void OnEnable()
        {
            if (!initialized)
                StartCoroutine(DeferredInitialize());
        }

        private System.Collections.IEnumerator DeferredInitialize()
        {
            yield return null; // Wait one frame for runtime-injected fields/services
            if (!initialized)
                InitializeData();
        }

        protected virtual void OnDestroy()
        {
            if (data == null) return;

            Query<DataAdapterRegistry<TData>>().Unregister(data);
            Query<IndexRegistry>().Unregister(data);

            bool ownsData = (data != dataSource || useAsTemplate || dataSource == null);
            if (ownsData)
                data.Release();
        }
    }
}
