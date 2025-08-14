using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Registries;
using ReaCS.Runtime.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using static ReaCS.Runtime.Access;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReaCS.Runtime.Core
{
    public abstract class ObservableObject : ScriptableObject, IPoolable, ICoreRegistrable, IRegistrable
    {
        public EntityId entityId;
        public event Action<ObservableObject, string> OnChanged;

        [SerializeField, HideInInspector] private int snapshotVersion = 1;
        public int SnapshotVersion => snapshotVersion;

        // Stable, serialized GUID per instance (used for filenames & version keys)
        [SerializeField, HideInInspector] private string persistentGuid;
        public string PersistentGuid => persistentGuid;

        // Guards
        [NonSerialized] private bool _isSnapshotClone;
        private static int _jsonCloneGuard;
        [NonSerialized] private bool _suppressSaveOnDisable;

        private static readonly Dictionary<Type, List<CachedFieldInfo>> _fieldCache = new();
        private List<CachedFieldInfo> _observedFields;
        private Dictionary<string, object> _cachedValues = new();
        private IPool _pool;

        public void SetPool(IPool pool) => _pool = pool;

        // Name injection (used by factory to set .name before OnEnable runs)
        private static string _pendingName;
        private static bool _hasPendingName;

        public readonly struct NameInjectionScope : IDisposable
        {
            public NameInjectionScope(string name)
            {
                _pendingName = name;
                _hasPendingName = true;
            }
            public void Dispose()
            {
                _pendingName = null;
                _hasPendingName = false;
            }
        }

        // --- pending GUID injection (like NameInjectionScope) ---
        private static string _pendingGuid;
        private static bool _hasPendingGuid;

        public readonly struct GuidInjectionScope : IDisposable
        {
            private readonly bool _armed;
            public GuidInjectionScope(string guid)
            {
                if (!string.IsNullOrEmpty(guid))
                {
                    _pendingGuid = guid;
                    _hasPendingGuid = true;
                    _armed = true;
                }
                else _armed = false;
            }
            public void Dispose()
            {
                if (_armed)
                {
                    _pendingGuid = null;
                    _hasPendingGuid = false;
                }
            }
        }



#if UNITY_EDITOR
        private static readonly Dictionary<ObservableObject, Dictionary<string, object>> _defaultValueCache = new();
#endif

        public virtual void OnEnable()
        {
            // apply injected name
            if (_hasPendingName && !string.IsNullOrEmpty(_pendingName))
            {
                this.name = _pendingName;
                _pendingName = null;
                _hasPendingName = false;
            }

            // apply injected GUID (BEFORE EnsurePersistentGuid)
            if (_hasPendingGuid && !string.IsNullOrEmpty(_pendingGuid))
            {
                persistentGuid = _pendingGuid;
                _pendingGuid = null;
                _hasPendingGuid = false;
            }

            // mark JSON clones / early returns etc...
            if (_jsonCloneGuard > 0) { _isSnapshotClone = true; return; }
            if (_isSnapshotClone) return;

            EnsurePersistentGuid(); // generates if still empty

            InitializeFields();
            Register();

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                LoadStateFromJson();
#else
    LoadStateFromJson();
#endif
        }

        protected virtual void OnDisable()
        {
            if (_isSnapshotClone) return;

            Unregister();

#if UNITY_EDITOR
            if (EditorApplication.isPlaying && !_suppressSaveOnDisable)
                SaveStateToJson();
#else
            if (!_suppressSaveOnDisable)
                SaveStateToJson();
#endif
            _suppressSaveOnDisable = false;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() => ProcessAllFields();
#endif

        public virtual void Initialize() => Register();

        public virtual void Release()
        {
            Unregister();
            _pool?.Release(this);
        }

        public void Register()
        {
            ObservableRegistry.Register(this);
            Query<IndexRegistry>().Register(this);
            RegisterSelf();
        }

        public void Unregister()
        {
            ObservableRegistry.Unregister(this);
            Query<IndexRegistry>().Unregister(this);
            UnregisterSelf();
        }

        public abstract void RegisterSelf();
        public abstract void UnregisterSelf();

        // ---------------- GUID + PATHS ----------------

        public void EnsurePersistentGuid()
        {
            if (string.IsNullOrEmpty(persistentGuid))
            {
                persistentGuid = System.Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
                // Persist into asset if this is an asset instance
                EditorUtility.SetDirty(this);
#endif
            }
        }

        private string GetSaveDirectory()
        {
#if UNITY_EDITOR
            return EditorApplication.isPlaying
                ? Application.persistentDataPath
                : Path.Combine(Directory.GetCurrentDirectory(), "Temp");
#else
            return Application.persistentDataPath;
#endif
        }

        private string GetFileSuffix()
        {
#if UNITY_EDITOR
            return EditorApplication.isPlaying ? "_state.json" : "_snapshot.json";
#else
            return "_state.json";
#endif
        }

        private string ComposeGuidPath()
        {
            var dir = GetSaveDirectory();
            var suffix = GetFileSuffix();
            var file = $"{name}_{persistentGuid}{suffix}";
            return Path.Combine(dir, file);
        }

        private string GetVersionKey() => $"{persistentGuid}_snapshot_version";

        // ---------------- Observable field setup ----------------

        private void InitializeFields()
        {
            Type type = GetType();

            if (!_fieldCache.TryGetValue(type, out var cachedFields))
            {
                cachedFields = new();
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!Attribute.IsDefined(field, typeof(ObservableAttribute))) continue;

                    var valueProp = field.FieldType.GetProperty("Value");
                    var persistField = field.FieldType.GetField("ShouldPersist",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    cachedFields.Add(new CachedFieldInfo
                    {
                        Field = field,
                        ValueProperty = valueProp,
                        ShouldPersistField = persistField
                    });
                }
                _fieldCache[type] = cachedFields;
            }

            _observedFields = cachedFields;

            foreach (var cached in _observedFields)
            {
                var field = cached.Field;
                var value = field.GetValue(this);

                if (value == null)
                {
                    value = Activator.CreateInstance(field.FieldType);
                    field.SetValue(this, value);
                }

                var initMethod = field.FieldType.GetMethod("Init");
                initMethod?.Invoke(value, new object[] { this, field.Name });

                var currentVal = cached.ValueProperty?.GetValue(value);
                _cachedValues[field.Name] = currentVal;
            }
        }

        internal void ProcessFieldChange(string fieldName)
        {
            if (_observedFields == null || _cachedValues == null) return;

            foreach (var cached in _observedFields)
            {
                if (cached.Field.Name != fieldName) continue;

                var obs = cached.Field.GetValue(this);
                var newValue = cached.ValueProperty?.GetValue(obs);

                if (!_cachedValues.TryGetValue(fieldName, out var oldValue) || !Equals(oldValue, newValue))
                {
                    _cachedValues[fieldName] = newValue;
                    OnChanged?.Invoke(this, fieldName);
                }
                break;
            }
        }

        internal void ProcessAllFields()
        {
            if (_observedFields == null || _cachedValues == null) return;

            foreach (var cached in _observedFields)
            {
                var obs = cached.Field.GetValue(this);
                var newValue = cached.ValueProperty?.GetValue(obs);

                if (!_cachedValues.TryGetValue(cached.Field.Name, out var oldValue) || !Equals(oldValue, newValue))
                {
                    _cachedValues[cached.Field.Name] = newValue;
                    OnChanged?.Invoke(this, cached.Field.Name);
                }
            }
        }

        internal IEnumerable<string> GetObservedFieldNames()
        {
            if (_observedFields == null) yield break;
            foreach (var cached in _observedFields)
                yield return cached.Field.Name;
        }

        internal object GetObservedFieldValue(string fieldName)
        {
            if (_observedFields == null) return null;
            foreach (var cached in _observedFields)
                if (cached.Field.Name == fieldName)
                    return cached.ValueProperty?.GetValue(cached.Field.GetValue(this));
            return null;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void SetupEditorResetHook()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode) CacheDefaultValues();
                else if (state == PlayModeStateChange.ExitingPlayMode) RestoreDefaultValues();
            };
        }

        private static void CacheDefaultValues()
        {
            _defaultValueCache.Clear();
            foreach (var so in Resources.FindObjectsOfTypeAll<ObservableObject>())
            {
                if (!_defaultValueCache.ContainsKey(so))
                    _defaultValueCache[so] = new();

                foreach (var cached in so._observedFields ?? new List<CachedFieldInfo>())
                {
                    var obs = cached.Field.GetValue(so);
                    var val = cached.ValueProperty?.GetValue(obs);
                    _defaultValueCache[so][cached.Field.Name] = val;
                }
            }
        }

        private static void RestoreDefaultValues()
        {
            foreach (var so in _defaultValueCache.Keys)
            {
                if (so == null) continue;

                foreach (var cached in so._observedFields ?? new List<CachedFieldInfo>())
                {
                    if (cached.ShouldPersistField == null) continue;

                    bool shouldPersist = (bool)(cached.ShouldPersistField.GetValue(cached.Field.GetValue(so)) ?? false);

                    if (!shouldPersist && _defaultValueCache[so].TryGetValue(cached.Field.Name, out var val))
                    {
                        var obs = cached.Field.GetValue(so);
                        ObservablePlayModeGuard.Suppress = true;
                        cached.ValueProperty?.SetValue(obs, val);
                        ObservablePlayModeGuard.Suppress = false;
                    }
                }
            }
            _defaultValueCache.Clear();
        }
#endif

        // ---------------- Save / Load (GUID-only) ----------------

        public void SaveStateToJson(bool log = true)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    Debug.LogError($"[ReaCS] {GetType().Name} has no name — cannot save.");
                    return;
                }
                EnsurePersistentGuid();

                var path = ComposeGuidPath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonUtility.ToJson(this);
                File.WriteAllText(path, json);

                PlayerPrefs.SetInt(GetVersionKey(), snapshotVersion);
                PlayerPrefs.Save();

#if UNITY_EDITOR
                if (log) Debug.Log($"[ReaCS] Saved '{name}' ({persistentGuid}) → {path}");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReaCS] Failed to save '{name}': {ex}");
            }
        }

        private static bool _isLoadingJson = false;

        public void LoadStateFromJson(bool log = true)
        {
            if (_isLoadingJson) return;
            _isLoadingJson = true;

            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    if (log) Debug.LogWarning($"[ReaCS] {GetType().Name} has no name on load — skipping.");
                    return;
                }

                EnsurePersistentGuid();

                string path = ComposeGuidPath();
                if (!File.Exists(path))
                {
#if UNITY_EDITOR
                    if (log) Debug.Log($"[ReaCS] No save found for '{name}' ({persistentGuid}) at {path}");
#endif
                    return;
                }

                int savedVersion = PlayerPrefs.GetInt(GetVersionKey(), -1);
                if (savedVersion != snapshotVersion)
                {
                    if (log) Debug.Log($"[ReaCS] Snapshot invalidated for {name} ({persistentGuid}) (v{savedVersion} → v{snapshotVersion}). Deleting.");
                    File.Delete(path);
                    PlayerPrefs.SetInt(GetVersionKey(), snapshotVersion);
                    PlayerPrefs.Save();
                    return;
                }

                var json = File.ReadAllText(path);

                // Create inert clone to import JSON
                _jsonCloneGuard++;
                var clone = ScriptableObject.CreateInstance(GetType()) as ObservableObject;
                _jsonCloneGuard--;
                if (clone == null) return;

                clone._isSnapshotClone = true;
                clone.hideFlags = HideFlags.HideAndDontSave;

                JsonUtility.FromJsonOverwrite(json, clone);

                // Copy persisted fields from snapshot into this instance
                foreach (var cached in _observedFields)
                {
                    var field = cached.Field;
                    var sourceObs = field.GetValue(clone);
                    var targetObs = field.GetValue(this);
                    if (sourceObs == null || targetObs == null) continue;

                    bool sourcePersist = cached.ShouldPersistField != null &&
                                         (bool)(cached.ShouldPersistField.GetValue(sourceObs) ?? false);
                    bool targetPersist = cached.ShouldPersistField != null &&
                                         (bool)(cached.ShouldPersistField.GetValue(targetObs) ?? false);

                    if (sourcePersist || targetPersist)
                    {
                        var value = cached.ValueProperty?.GetValue(sourceObs);
                        cached.ValueProperty?.SetValue(targetObs, value);
                    }
                }

#if UNITY_EDITOR
                DestroyImmediate(clone);
#else
                Destroy(clone);
#endif

#if UNITY_EDITOR
                if (log) Debug.Log($"[ReaCS] Loaded '{name}' ({persistentGuid}) ← {path}");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReaCS] Failed to load '{name}': {ex}");
            }
            finally
            {
                _isLoadingJson = false;
            }
        }

        public bool DeleteStateOnDisk(bool log = true)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    if (log) Debug.LogWarning("[ReaCS] DeleteStateOnDisk skipped: object has no name.");
                    return false;
                }
                EnsurePersistentGuid();

                string path = ComposeGuidPath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                    if (log) Debug.Log($"[ReaCS] Deleted snapshot: {path}");
                }
                PlayerPrefs.DeleteKey(GetVersionKey());
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReaCS] DeleteStateOnDisk failed for '{name}': {ex}");
                return false;
            }
        }

        public void SuppressSaveOnDisableOnce() => _suppressSaveOnDisable = true;

#if UNITY_EDITOR
        public void BumpSnapshotVersion()
        {
            snapshotVersion++;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif

        // ---------- helper struct ----------
        private class CachedFieldInfo
        {
            public FieldInfo Field;
            public PropertyInfo ValueProperty;
            public FieldInfo ShouldPersistField;
        }
    }
}
