using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Registries;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    public abstract class Reactor<TSO> : MonoBehaviour where TSO : ObservableObject
    {
        private string _observedField;

        private struct WatchedEntry
        {
            public TSO so;
            public ObservableBase observable;
            public int lastHash;
        }

        private List<WatchedEntry> _watched = new();
        private NativeArray<int> _prevHashes;
        private NativeArray<int> _currHashes;

        private bool IsInActiveScene()
        {
            return this != null && gameObject != null && gameObject.scene.IsValid() && gameObject.activeInHierarchy;
        }

        protected virtual void Start()
        {
            if (!Application.isPlaying || !IsInActiveScene()) return;

            _observedField = ResolveObservedField();
            if (string.IsNullOrEmpty(_observedField)) return;

            ObservableRegistry.OnRegistered += HandleNewSO;
            ObservableRegistry.OnUnregistered += HandleRemovedSO;

            SubscribeAll();
        }

        protected virtual void OnDestroy()
        {
            if (!Application.isPlaying) return;

            ObservableRegistry.OnRegistered -= HandleNewSO;
            ObservableRegistry.OnUnregistered -= HandleRemovedSO;

            DisposeHashArrays();
            _watched.Clear();
        }

        private void SubscribeAll()
        {
            foreach (var so in ObservableRegistry.GetAll<TSO>())
            {
                if (IsTarget(so)) TryWatchField(so);
            }
            AllocateHashes();
        }

        private void HandleNewSO(ObservableObject obj)
        {
            if (obj is TSO so && IsTarget(so))
            {
                TryWatchField(so);
                AllocateHashes();
            }
        }

        private void HandleRemovedSO(ObservableObject obj)
        {
            if (obj is TSO so)
            {
                _watched.RemoveAll(w => w.so == so);
                AllocateHashes();
            }
        }

        private void TryWatchField(TSO so)
        {
            var fieldInfo = typeof(TSO).GetField(_observedField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo?.GetValue(so) is ObservableBase observable)
            {
                _watched.Add(new WatchedEntry
                {
                    so = so,
                    observable = observable,
                    lastHash = observable.FastHashValue
                });
                ReaCSDebug.Log($"[ReaCS] {GetType().Name} is watching {so.name}.{_observedField}");
            }
        }

        private void AllocateHashes()
        {
            DisposeHashArrays();

            int count = _watched.Count;
            if (count == 0) return;

            _prevHashes = new NativeArray<int>(count, Allocator.Persistent);
            _currHashes = new NativeArray<int>(count, Allocator.Persistent);

            for (int i = 0; i < count; i++)
            {
                _prevHashes[i] = _watched[i].lastHash;
                _currHashes[i] = _watched[i].observable.FastHashValue;
            }
        }

        private void DisposeHashArrays()
        {
            if (_prevHashes.IsCreated) _prevHashes.Dispose();
            if (_currHashes.IsCreated) _currHashes.Dispose();
        }

        [BurstCompile]
        private struct CheckFieldChangesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> Prev;
            [ReadOnly] public NativeArray<int> Curr;
            public NativeArray<byte> Dirty;

            public void Execute(int index)
            {
                Dirty[index] = Prev[index] != Curr[index] ? (byte)1 : (byte)0;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || _watched.Count == 0) return;

            for (int i = 0; i < _watched.Count; i++)
                _currHashes[i] = _watched[i].observable.FastHashValue;

            var dirtyFlags = new NativeArray<byte>(_watched.Count, Allocator.TempJob);

            var job = new CheckFieldChangesJob
            {
                Prev = _prevHashes,
                Curr = _currHashes,
                Dirty = dirtyFlags
            };

            job.Schedule(_watched.Count, 64).Complete();

            for (int i = 0; i < _watched.Count; i++)
            {
                if (dirtyFlags[i] == 1)
                {
                    OnFieldChanged(_watched[i].so);
                    _prevHashes[i] = _currHashes[i];
                }
            }

            dirtyFlags.Dispose();
        }

        protected abstract void OnFieldChanged(TSO changedSO);
        protected virtual bool IsTarget(TSO so) => true;

        private string ResolveObservedField()
        {
            var attr = GetType().GetCustomAttribute<ReactToAttribute>();
            if (attr == null || string.IsNullOrWhiteSpace(attr.FieldName))
            {
                ReaCSDebug.LogWarning($"[ReaCS] {GetType().Name} is missing a valid [ReactTo] attribute.");
                return null;
            }

            ReaCSDebug.Log($"[ReaCS] {GetType().Name} is observing field: {attr.FieldName}");
            return attr.FieldName;
        }
    }
}