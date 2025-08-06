using ReaCS.Runtime.Internal;
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

        private static Dictionary<IHasFastHash, int> _observableToId = new();
        private static List<IHasFastHash> _idToObservable = new();

        private static NativeArray<int> _previousHashes;
        private static NativeArray<int> _currentHashes;
        private static NativeArray<byte> _dirtyFlags;
        private static int _capacity = 1024;
        private static int _registeredCount = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
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

            _previousHashes = new NativeArray<int>(newSize, Allocator.Persistent);
            _currentHashes = new NativeArray<int>(newSize, Allocator.Persistent);
            _dirtyFlags = new NativeArray<byte>(newSize, Allocator.Persistent);

            if (oldCount > 0)
            {
                NativeArray<int>.Copy(oldPrev, _previousHashes, oldCount);
                NativeArray<int>.Copy(oldCurr, _currentHashes, oldCount);
                NativeArray<byte>.Copy(oldDirty, _dirtyFlags, oldCount);

                oldPrev.Dispose();
                oldCurr.Dispose();
                oldDirty.Dispose();
            }
        }

        public static void Register(IHasFastHash observable)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#else
            if (!Application.isPlaying) return;
#endif
            if (!_isInitialized) Init();
            if (_observableToId.ContainsKey(observable)) return;

            int id = _registeredCount;
            _observableToId[observable] = id;
            _idToObservable.Add(observable);
            _registeredCount++;

            if (!_previousHashes.IsCreated || id >= _previousHashes.Length)
            {
                _capacity = Mathf.Max(_capacity * 2, id + 1);
                AllocateNativeArrays(_capacity);
            }

            int hash = observable.FastHashValue;
            _previousHashes[id] = hash;
            _currentHashes[id] = hash;
            _dirtyFlags[id] = 0;
        }

        public static void Unregister(IHasFastHash observable)
        {
            if (!_observableToId.TryGetValue(observable, out int id)) return;

            _observableToId.Remove(observable);
            if (id < _idToObservable.Count)
                _idToObservable[id] = null;

            if (_previousHashes.IsCreated && id < _previousHashes.Length)
            {
                _previousHashes[id] = 0;
                _currentHashes[id] = 0;
                _dirtyFlags[id] = 0;
            }
        }

        [BurstCompile]
        private struct CheckChangesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> Previous;
            [ReadOnly] public NativeArray<int> Current;
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

            for (int i = 0; i < _registeredCount; i++)
            {
                var obs = _idToObservable[i];
                if (obs == null)
                {
                    _currentHashes[i] = _previousHashes[i];
                    continue;
                }

                _currentHashes[i] = obs.FastHashValue;
            }

            var job = new CheckChangesJob
            {
                Previous = _previousHashes,
                Current = _currentHashes,
                DirtyFlags = _dirtyFlags
            };
            job.Schedule(_registeredCount, 64).Complete();

            for (int i = 0; i < _registeredCount; i++)
            {
                if (_dirtyFlags[i] == 1)
                {
                    if (_idToObservable[i] is ObservableBase baseObs)
                        baseObs.NotifyChanged();

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
            _observableToId.Clear();
            _idToObservable.Clear();
            _registeredCount = 0;
        }
    }
}
