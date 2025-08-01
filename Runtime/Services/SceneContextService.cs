using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ReaCS.Runtime.Core;
using static ReaCS.Runtime.Access;

namespace ReaCS.Runtime.Services
{
    public class SceneContextService : IReaCSService
    {
        private readonly Dictionary<string, List<ObservableObject>> _sceneMap = new();

        public SceneContextService()
        {
            SceneManager.sceneUnloaded += scene =>
            {
                if (_sceneMap.ContainsKey(scene.name))
                {
                    Debug.Log($"[SceneContextService] Scene '{scene.name}' unloaded — cleaning up runtime OSOs and links.");
                    Unload(scene.name);
                }
            };
        }

        public void Register(string sceneName, ObservableObject oso)
        {
            if (!_sceneMap.TryGetValue(sceneName, out var list))
                _sceneMap[sceneName] = list = new();

            if (!list.Contains(oso))
                list.Add(oso);
        }

        public void Unload(string sceneName)
        {
            if (!_sceneMap.TryGetValue(sceneName, out var list)) return;

            foreach (var oso in list)
            {
                if (oso == null) continue;
                if (oso.hideFlags.HasFlag(HideFlags.DontSaveInEditor) ||
                    oso.hideFlags.HasFlag(HideFlags.DontSaveInBuild))
                {
                    var poolType = typeof(PoolService<>).MakeGenericType(oso.GetType());
                    if (TryUse(poolType, out var poolInstance))
                    {
                        var releaseMethod = poolType.GetMethod("Release");
                        releaseMethod?.Invoke(poolInstance, new object[] { oso });
                    }
                }
            }

            _sceneMap.Remove(sceneName);
        }

        public void ClearAll()
        {
            var allScenes = new List<string>(_sceneMap.Keys);
            foreach (var scene in allScenes)
                Unload(scene);
        }
    }
}
