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
        /// This overload infers the binding and link types.
        /// </summary>
        public void BindAll<TSOParent, TSOChild, TUC>(
            GameObject root,
            TSOParent sharedSO)
            where TSOParent : ObservableScriptableObject
            where TSOChild : ObservableScriptableObject, new()
            where TUC : Component
        {
            // Infer concrete types
            var bindingType = typeof(ComponentDataBinding<TSOChild, TUC>);
            var linkType = typeof(LinkSO<TSOParent, TSOChild>);

            int childCount = 0;
            foreach (var target in root.GetComponentsInChildren<TUC>(true))
            {
                var go = target.gameObject;

                // Add or get binding
                var binding = (ComponentDataBinding<TSOChild, TUC>)go.GetComponent(bindingType);
                if (binding == null)
                    binding = (ComponentDataBinding<TSOChild, TUC>)go.AddComponent(bindingType);

                // Initialize and retrieve child SO
                binding.InitializeData();
                var childSO = binding.data;

                // Create link
                var link = (LinkSO<TSOParent, TSOChild>)ScriptableObject.CreateInstance(linkType);
                link.name = $"{sharedSO.name}_link_{childCount}";
                link.LeftSO.Value = sharedSO;
                link.RightSO.Value = childSO;
                Access.Query<LinkSORegistry>().Register(link);

                childCount++;
            }
        }

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

                // 1. Add or get binding
                var binding = go.GetComponent<TBinding>() ?? go.AddComponent<TBinding>();

                // 2. Force binding to initialize its pooled data
                binding.InitializeData();

                // 3. Get that auto-pooled child SO
                var childSO = binding.data;

                // 4. Create link to shared SO
                var link = ScriptableObject.CreateInstance<TLink>();
                link.name = $"{sharedSO.name}_link_{childCount}";
                link.LeftSO.Value = sharedSO;
                link.RightSO.Value = childSO;
                Access.Query<LinkSORegistry>().Register(link);
                childCount++;
            }
        }

    }
}
