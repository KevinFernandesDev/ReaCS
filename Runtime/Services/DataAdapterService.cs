using UnityEngine;
using ReaCS.Runtime.Core;
using EntityId = ReaCS.Runtime.Core.EntityId;

namespace ReaCS.Runtime.Services
{
    public class DataAdapterService : IReaCSService
    {
        /// <summary>
        /// Adds and initializes a DataAdapter component to a GameObject.
        /// Optionally assigns a data source, EntityId, and useAsTemplate flag.
        /// Will never cause SetActive glitches!
        /// </summary>
        public TAdapter Create<TData, TUC, TAdapter>(
            GameObject go,
            EntityId? entityId = null,
            TData dataSource = null,
            bool useAsTemplate = false)
            where TData : Data, new()
            where TUC : Component
            where TAdapter : DataAdapter<TData, TUC>
        {
            var adapter = go.AddComponent<TAdapter>();
            adapter.SetupForRuntime(entityId, dataSource, useAsTemplate);
            adapter.InitializeData();
            return adapter;
        }
    }
}
