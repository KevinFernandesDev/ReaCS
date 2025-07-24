using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    [DefaultExecutionOrder(-10000)]
    public class ObservableRuntimeWatcher : MonoBehaviour
    {
        private static ObservableRuntimeWatcher _instance;
        private static bool _isInitialized = false;

        private static Dictionary<ObservableScriptableObject, int> _objectToId = new();
        private static List<ObservableScriptableObject> _idToObject = new();

        // ✅ Event-driven dirty fields
        private static List<(int, string)> _dirtyFields = new(256);

        // ✅ HighFrequency Burst job data
        private static NativeArray<float> _previousHashes;
        private static NativeArray<float> _currentHashes;
        private static NativeArray<byte> _dirtyFlags;
        private static int _capacity = 1024;
        private static int _registeredCount = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Init()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (_isInitialized) return;

            if (_instance == null)
            {
                var go = new GameObject("ObservableRuntimeWatcher");
                _instance = go.AddComponent<ObservableRuntimeWatcher>();
                DontDestroyOnLoad(go);
            }

            if (!_previousHashes.IsCreated)
                AllocateNativeArrays(_capacity);

            _isInitialized = true;
        }


        private static void AllocateNativeArrays(int newSize)
        {
            var oldPrev = _previousHashes;
            var oldCurr = _currentHashes;
            var oldDirty = _dirtyFlags;

            int oldCount = oldPrev.IsCreated ? oldPrev.Length : 0;

            _previousHashes = new NativeArray<float>(newSize, Allocator.Persistent);
            _currentHashes = new NativeArray<float>(newSize, Allocator.Persistent);
            _dirtyFlags = new NativeArray<byte>(newSize, Allocator.Persistent);

            if (oldCount > 0)
            {
                NativeArray<float>.Copy(oldPrev, _previousHashes, oldCount);
                NativeArray<float>.Copy(oldCurr, _currentHashes, oldCount);
                NativeArray<byte>.Copy(oldDirty, _dirtyFlags, oldCount);

                oldPrev.Dispose();
                oldCurr.Dispose();
                oldDirty.Dispose();
            }
        }

        public static void Register(ObservableScriptableObject so)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#else
    if (!Application.isPlaying) return;
#endif
            if (!_isInitialized) Init();
            if (_objectToId.ContainsKey(so)) return;

            int id = _registeredCount;
            _objectToId[so] = id;
            _idToObject.Add(so);
            _registeredCount++;

            // ✅ Ensure arrays are allocated before use
            if (!_previousHashes.IsCreated || id >= _previousHashes.Length)
            {
                _capacity = Mathf.Max(_capacity * 2, id + 1);
                AllocateNativeArrays(_capacity);
            }

            float hash = so.updateMode == UpdateMode.HighFrequency ? so.ComputeFastHash() : 0f;
            _previousHashes[id] = hash;
            _currentHashes[id] = hash;
            _dirtyFlags[id] = 0;
        }


        public static void Unregister(ObservableScriptableObject so)
        {
            if (!_objectToId.TryGetValue(so, out int id)) return;

            _objectToId.Remove(so);
            if (id < _idToObject.Count)
                _idToObject[id] = null;

            for (int i = _dirtyFields.Count - 1; i >= 0; i--)
            {
                if (_dirtyFields[i].Item1 == id)
                    _dirtyFields.RemoveAt(i);
            }

            if (_previousHashes.IsCreated && id < _previousHashes.Length)
            {
                _previousHashes[id] = 0f;
                _currentHashes[id] = 0f;
                _dirtyFlags[id] = 0;
            }
        }

        public static void NotifyDirty(ObservableScriptableObject so, string fieldName)
        {
            if (!_objectToId.TryGetValue(so, out int id)) return;

            for (int i = 0; i < _dirtyFields.Count; i++)
            {
                if (_dirtyFields[i].Item1 == id && _dirtyFields[i].Item2 == fieldName)
                    return;
            }
            _dirtyFields.Add((id, fieldName));
        }

        [BurstCompile]
        private struct CheckChangesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> Previous;
            [ReadOnly] public NativeArray<float> Current;
            public NativeArray<byte> DirtyFlags;

            public void Execute(int index)
            {
                DirtyFlags[index] = (Previous[index] != Current[index]) ? (byte)1 : (byte)0;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (_registeredCount == 0) return;

            // ✅ Process Event-Driven Dirty Fields
            for (int i = 0; i < _dirtyFields.Count; i++)
            {
                int soId = _dirtyFields[i].Item1;
                string fieldName = _dirtyFields[i].Item2;

                if (soId >= 0 && soId < _idToObject.Count)
                {
                    var so = _idToObject[soId];
                    so?.ProcessFieldChange(fieldName);
                }
            }
            _dirtyFields.Clear();

            // ✅ Update hashes only for HighFrequency SOs
            for (int i = 0; i < _registeredCount; i++)
            {
                var so = _idToObject[i];
                if (so == null || so.updateMode != UpdateMode.HighFrequency)
                {
                    _currentHashes[i] = _previousHashes[i];
                    continue;
                }

                try
                {
                    _currentHashes[i] = so.ComputeFastHash();
                }
                catch
                {
                    _currentHashes[i] = 0f;
                }
            }

            // ✅ Run Burst Parallel Job
            var job = new CheckChangesJob
            {
                Previous = _previousHashes,
                Current = _currentHashes,
                DirtyFlags = _dirtyFlags
            };
            job.Schedule(_registeredCount, 64).Complete();

            // ✅ Apply Changes for Dirty HighFrequency SOs
            for (int i = 0; i < _registeredCount; i++)
            {
                if (_dirtyFlags[i] == 1)
                {
                    var so = _idToObject[i];
                    if (so != null)
                        so.ProcessAllFields();

                    _previousHashes[i] = _currentHashes[i];
                    _dirtyFlags[i] = 0;
                }
            }
        }

        private void OnDestroy()
        {
            if (_previousHashes.IsCreated) _previousHashes.Dispose();
            if (_currentHashes.IsCreated) _currentHashes.Dispose();
            if (_dirtyFlags.IsCreated) _dirtyFlags.Dispose();

            _isInitialized = false;
            _objectToId.Clear();
            _idToObject.Clear();
            _dirtyFields.Clear();
            _registeredCount = 0;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void ForceUpdateAll()
        {
            for (int i = 0; i < _idToObject.Count; i++)
            {
                var so = _idToObject[i];
                if (so == null) continue;
                so.ProcessAllFields();
            }
        }
#endif
    }
}
