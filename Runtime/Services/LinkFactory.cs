using ReaCS.Runtime.Core;
using System;
using UnityEngine;

namespace ReaCS.Runtime.Services
{
    public class LinkFactory : IReaCSService
    {
        /// <summary>
        /// Creates and registers a new LinkSO between two ObservableScriptableObjects.
        /// </summary>
        public static LinkSO<TLeft, TRight> Create<TLeft, TRight>(TLeft left, TRight right, string name = null)
            where TLeft : ObservableScriptableObject
            where TRight : ObservableScriptableObject
        {
            if (left == null )
                throw new ArgumentNullException($"[LinkFactory] {name}: left is null");
            else if(right == null)
                throw new ArgumentNullException($"[LinkFactory] {name}:  right is null");

            var link = ScriptableObject.CreateInstance<LinkSO<TLeft, TRight>>();
            link.name = name ?? $"{left.name}_to_{right.name}";
            link.SetLinks(left, right);

            return link;
        }
    }
}