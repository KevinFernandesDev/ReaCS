using System;
using UnityEngine;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;

namespace ReaCS.Runtime.Services
{
    public class LinkBindingService : IReaCSService
    {
        /// <summary>
        /// Binds all matching Unity components to a shared parent SO via Link,
        /// and creates per-object child SOs with typed data bindings.
        /// </summary>
        public void BindAll<TSOParent, TSOChild, TUC, TBinding, TLink>(
            GameObject root,
            TSOParent sharedSO)
            where TSOParent : ObservableObject
            where TSOChild : Data, new()
            where TUC : Component
            where TBinding : DataAdapter<TSOChild, TUC>
            where TLink : Link<TSOParent, TSOChild>, ILinkConnector // Use your new interface name
        {
            int childCount = 0;
            foreach (var target in root.GetComponentsInChildren<TUC>(true))
            {
                var go = target.gameObject;

                // Add or get binding
                var binding = go.GetComponent<TBinding>() ?? go.AddComponent<TBinding>();

                // Force binding to initialize its pooled data
                binding.InitializeData();

                // Get that auto-pooled child SO
                var childSO = binding.data;

                // Create link to shared SO using new LinkPool
                var link = Access.Use<LinkPool<TLink>>().Get(sharedSO, childSO);
                link.name = $"[BindAll]_{sharedSO.name}_to_{childSO.name}_{childCount}";
                childCount++;
            }
        }
    }
}
