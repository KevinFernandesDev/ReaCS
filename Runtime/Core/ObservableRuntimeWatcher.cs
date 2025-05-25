using ReaCS.Runtime.Internal;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace ReaCS.Runtime
{
    [DefaultExecutionOrder(-10000)]
    public class ObservableRuntimeWatcher : MonoBehaviour
    {
        private static ObservableRuntimeWatcher _instance;

        private static Dictionary<ObservableScriptableObject, int> _objectToId = new();
        private static List<ObservableScriptableObject> _idToObject = new();

        private static NativeList<int> _debouncedIds;
        private static NativeHashMap<int, float> _debounceTimers;
        private static NativeList<int> _readyToUpdate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _objectToId.Clear();
            _idToObject.Clear();

            if (_debouncedIds.IsCreated) _debouncedIds.Dispose();
            if (_debounceTimers.IsCreated) _debounceTimers.Dispose();
            if (_readyToUpdate.IsCreated) _readyToUpdate.Dispose();

            _debouncedIds = new NativeList<int>(100, Allocator.Persistent);
            _debounceTimers = new NativeHashMap<int, float>(100, Allocator.Persistent);
            _readyToUpdate = new NativeList<int>(100, Allocator.Persistent);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Init()
        {
            if (_instance == null)
            {
                var go = new GameObject("ObservableRuntimeWatcher");
                _instance = go.AddComponent<ObservableRuntimeWatcher>();
                DontDestroyOnLoad(go);
            }
        }

        public static void Register(ObservableScriptableObject so)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#else
            if (!Application.isPlaying) return;
#endif
            if (_objectToId.ContainsKey(so)) return;

            int id = _idToObject.Count;
            _objectToId[so] = id;
            _idToObject.Add(so);
        }

        public static void Unregister(ObservableScriptableObject so)
        {
            if (_objectToId.TryGetValue(so, out int id))
            {
                _objectToId.Remove(so);
                // Note: we leave _idToObject alone for safety (sparse array)
                _debounceTimers.Remove(id);
                RemoveId(_debouncedIds, id);
                RemoveId(_readyToUpdate, id);
            }
        }

        private static void RemoveId(NativeList<int> list, int id)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == id)
                {
                    list.RemoveAtSwapBack(i);
                    return;
                }
            }
        }

        public static void DebounceChange(ObservableScriptableObject so, float delay)
        {
            if (!_objectToId.TryGetValue(so, out int id)) return;

            if (!_debouncedIds.Contains(id))
                _debouncedIds.Add(id);

            _debounceTimers[id] = delay;
        }

        [BurstCompile]
        private struct TickDebounceJob : IJob
        {
            public float DeltaTime;
            public NativeList<int> DebouncedIds;
            public NativeHashMap<int, float> Timers;
            public NativeList<int> ReadyIds;

            public void Execute()
            {
                for (int i = 0; i < DebouncedIds.Length; i++)
                {
                    int id = DebouncedIds[i];
                    if (!Timers.TryGetValue(id, out float time)) continue;

                    time -= DeltaTime;
                    if (time <= 0f)
                    {
                        ReadyIds.Add(id);
                        Timers.Remove(id);
                        DebouncedIds.RemoveAtSwapBack(i);
                        i--; // Adjust index due to swap
                    }
                    else
                    {
                        Timers[id] = time;
                    }
                }
            }
        }

        private void Update()
        {
            if (_debouncedIds.Length == 0)
                return;

            var job = new TickDebounceJob
            {
                DeltaTime = Time.deltaTime,
                DebouncedIds = _debouncedIds,
                Timers = _debounceTimers,
                ReadyIds = _readyToUpdate
            };

            job.Run();

            for (int i = 0; i < _readyToUpdate.Length; i++)
            {
                int id = _readyToUpdate[i];
                if (id >= 0 && id < _idToObject.Count)
                {
                    var so = _idToObject[id];
                    so?.CheckForChanges();
                }
            }

            _readyToUpdate.Clear();
        }

        private void OnDestroy()
        {
            if (_debouncedIds.IsCreated) _debouncedIds.Dispose();
            if (_debounceTimers.IsCreated) _debounceTimers.Dispose();
            if (_readyToUpdate.IsCreated) _readyToUpdate.Dispose();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void ForceUpdate()
        {
            foreach (var pair in _objectToId)
            {
                pair.Key?.CheckForChanges();
            }
        }
#endif
#if UNITY_EDITOR
        public static void MarkAllDirty()
        {
            foreach (var pair in _objectToId)
            {
                pair.Key?.MarkDirty("TestDirtyAll");
            }
        }
#endif
    }
}
