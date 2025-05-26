#if UNITY_EDITOR
using ReaCS.Runtime;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static ReaCS.Runtime.ReaCS;

namespace ReaCS.Editor
{
    public static class LinkSOCleanupUtility
    {
        [MenuItem("Tools/ReaCS/Cleanup Broken LinkSOs")]
        public static void CleanupBrokenLinks()
        {
            var service = Query<LinkSORegistry>();
            int cleaned = 0;

            var allTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && IsLinkSO(t));

            foreach (var type in allTypes)
            {
                foreach (var link in service.GetAllLinksOfType(type))
                {
                    var leftField = type.GetField("Left");
                    var rightField = type.GetField("Right");

                    if (leftField == null || rightField == null) continue;

                    var left = leftField.GetValue(link);
                    var right = rightField.GetValue(link);

                    bool isBroken = false;

                    if (left is IObservableReference lRef && lRef.Value == null)
                        isBroken = true;
                    if (right is IObservableReference rRef && rRef.Value == null)
                        isBroken = true;

                    if (isBroken)
                    {
                        string path = AssetDatabase.GetAssetPath(link);
                        if (!string.IsNullOrEmpty(path))
                        {
                            AssetDatabase.DeleteAsset(path);
                            Debug.Log($"🧹 [ReaCS] Deleted broken LinkSO at: {path}");
                            cleaned++;
                        }
                    }
                }
            }

            if (cleaned == 0)
                Debug.Log("[ReaCS] No broken LinkSO assets found.");
        }

        private static bool IsLinkSO(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LinkSO<,>))
                    return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}
#endif
