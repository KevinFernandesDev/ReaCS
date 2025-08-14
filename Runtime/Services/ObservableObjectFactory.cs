using ReaCS.Runtime.Core;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using EntityId = ReaCS.Runtime.Core.EntityId;

namespace ReaCS.Runtime.Services
{
    public class ObservableObjectFactory : IObservableObjectFactory, IReaCSService
    {
        public T Create<T>(string name = null, EntityId? entityId = null) where T : ObservableObject
        {
            using (new ObservableObject.NameInjectionScope(name ?? typeof(T).Name))
            {
                var instance = ScriptableObject.CreateInstance<T>();

                if (entityId.HasValue)
                    instance.entityId = entityId.Value;

                return instance;
            }
        }

        public bool DeleteState(ObservableObject instance, bool log = true)
        {
            if (instance == null) return false;
            return instance.DeleteStateOnDisk(log);
        }

        public void Destroy(ObservableObject instance, bool deleteState = true, bool log = true)
        {
            if (instance == null) return;

            // delete JSON first (optional)
            if (deleteState)
                instance.DeleteStateOnDisk(log);

            // ensure we don't write a final save on disable
            instance.SuppressSaveOnDisableOnce();

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                UnityEngine.Object.Destroy(instance);
            else
                UnityEngine.Object.DestroyImmediate(instance);
#else
            UnityEngine.Object.Destroy(instance);
#endif
        }

        public int PurgeAllSnapshots(bool log = true)
        {
            int count = 0;

            // runtime / play mode snapshots
            try
            {
                var pdp = Application.persistentDataPath;
                if (Directory.Exists(pdp))
                {
                    foreach (var file in Directory.GetFiles(pdp, "*_state.json"))
                    {
                        try
                        {
                            File.Delete(file);
                            count++;
                            if (log) Debug.Log($"[ReaCS] Deleted {file}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

#if UNITY_EDITOR
            // edit-mode snapshots in Temp/
            try
            {
                var temp = Path.Combine(Directory.GetCurrentDirectory(), "Temp");
                if (Directory.Exists(temp))
                {
                    foreach (var file in Directory.GetFiles(temp, "*_snapshot.json"))
                    {
                        try
                        {
                            File.Delete(file);
                            count++;
                            if (log) Debug.Log($"[ReaCS] Deleted {file}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
#endif
            PlayerPrefs.Save();
            return count;
        }
    }
}
    }
}
