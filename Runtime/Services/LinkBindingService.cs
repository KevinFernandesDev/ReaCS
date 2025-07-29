using System;
using UnityEngine;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;

namespace ReaCS.Runtime.Services
{
    public class LinkBindingService : IReaCSService
    {
        /// <summary>
        /// Binds all matching Unity components to a shared parent SO via LinkSO,
        /// and creates per-object child SOs with typed data bindings.
        /// </summary>
        public void BindAll<TSOParent, TSOChild, TUC, TBinding, TLink>(
        GameObject root,
        TSOParent sharedSO)
        where TSOParent : ObservableScriptableObject
        where TSOChild : ObservableScriptableObject, new()
        where TUC : Component
        where TBinding : ComponentDataBinding<TSOChild, TUC>
        where TLink : LinkSO<TSOParent, TSOChild>
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

                // Create link to shared SO
                var link = Access.Use<PoolService<TLink>>().GetLink<TLink, TSOParent, TSOChild>(sharedSO, childSO);
                link.name = $"[BindAll]_{sharedSO.name}_to_{childSO.name}_{childCount}";
                childCount++;
            }
        }

    }
}
