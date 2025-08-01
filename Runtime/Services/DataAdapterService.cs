using UnityEngine;
using ReaCS.Runtime.Core;
using EntityId = ReaCS.Runtime.Core.EntityId;

namespace ReaCS.Runtime.Services
{
    public static class DataAdapterService
    {
        /// <summary>
        /// Adds and initializes a DataAdapter component to a GameObject.
        /// Optionally assigns a data source, EntityId, and useAsTemplate flag.
        /// Will never cause SetActive glitches!
        /// </summary>
        public static TAdapter Create<TSO, TUC, TAdapter>(
            GameObject go,
            Data dataSource = null,
            EntityId? entityId = null,
            bool useAsTemplate = false)
            where TSO : ObservableObject, new()
            where TUC : Component
            where TAdapter : DataAdapter<Data, TUC>
        {
            var adapter = go.AddComponent<TAdapter>();
            adapter.SetupForRuntime(entityId, dataSource, useAsTemplate);
            adapter.InitializeData();
            return adapter;
        }
    }
}
