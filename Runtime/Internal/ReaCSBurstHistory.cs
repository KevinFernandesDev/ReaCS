using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ReaCS.Runtime.Internal
{
    public enum ReaCSValueKind : byte
    {
        Float,
        Int,
        Bool,
        Vector2,
        Vector3,
        Unknown
    }

    public static class ReaCSBurstHistory
    {
        private static NativeList<BurstableHistoryEntry> _entries;
        private static readonly List<BurstableHistoryEntry> _backup = new();
        private static bool _initialized;

#if UNITY_EDITOR
        public static System.Action OnEditorLogUpdated;
#endif

        public static void Init()
        {
            if (_initialized && _entries.IsCreated)
                return;

            if (_entries.IsCreated)
                _entries.Dispose();

            _entries = new NativeList<BurstableHistoryEntry>(1024, Allocator.Persistent);
            foreach (var entry in _backup)
                _entries.Add(entry);

            _initialized = true;
        }

        public static void Clear()
        {
            if (_entries.IsCreated)
                _entries.Clear();
            _backup.Clear();
        }

        public static void Dispose()
        {
            if (_entries.IsCreated)
                _entries.Dispose();

            _backup.Clear();
            _initialized = false;
        }

        public static BurstableHistoryEntry[] ToArray()
        {
            if (!_entries.IsCreated) return null;

            var result = new BurstableHistoryEntry[_entries.Length];
            _entries.AsArray().CopyTo(result);
            return result;
        }

        private static void Add(BurstableHistoryEntry entry)
        {
            if (!_entries.IsCreated)
            {
                Debug.LogWarning("ReaCSBurstHistory: Attempted to log without a valid history buffer.");
                return;
            }

            _entries.Add(entry);
            _backup.Add(entry);

#if UNITY_EDITOR
            OnEditorLogUpdated?.Invoke();
#endif
        }

        public static void LogFloat(FixedString64Bytes so, FixedString64Bytes field, float oldVal, float newVal, FixedString64Bytes system)
        {
            Add(new BurstableHistoryEntry
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Float,
                valueOld = new float3(oldVal, 0, 0),
                valueNew = new float3(newVal, 0, 0),
#if UNITY_EDITOR
                debugOld = oldVal.ToString("F3"),
                debugNew = newVal.ToString("F3")
#endif
            });
        }

        public static void LogInt(FixedString64Bytes so, FixedString64Bytes field, int oldVal, int newVal, FixedString64Bytes system)
        {
            Add(new BurstableHistoryEntry
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Int,
                valueOld = new float3(oldVal, 0, 0),
                valueNew = new float3(newVal, 0, 0),
#if UNITY_EDITOR
                debugOld = oldVal.ToString(),
                debugNew = newVal.ToString()
#endif
            });
        }

        public static void LogBool(FixedString64Bytes so, FixedString64Bytes field, bool oldVal, bool newVal, FixedString64Bytes system)
        {
            Add(new BurstableHistoryEntry
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Bool,
                valueOld = new float3(oldVal ? 1 : 0, 0, 0),
                valueNew = new float3(newVal ? 1 : 0, 0, 0),
#if UNITY_EDITOR
                debugOld = oldVal.ToString(),
                debugNew = newVal.ToString()
#endif
            });
        }

        public static void LogVector2(FixedString64Bytes so, FixedString64Bytes field, Vector2 oldVal, Vector2 newVal, FixedString64Bytes system)
        {
            Add(new BurstableHistoryEntry
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Vector2,
                valueOld = new float3(oldVal.x, oldVal.y, 0),
                valueNew = new float3(newVal.x, newVal.y, 0),
#if UNITY_EDITOR
                debugOld = oldVal.ToString(),
                debugNew = newVal.ToString()
#endif
            });
        }

        public static void LogVector3(FixedString64Bytes so, FixedString64Bytes field, Vector3 oldVal, Vector3 newVal, FixedString64Bytes system)
        {
            Add(new BurstableHistoryEntry
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Vector3,
                valueOld = new float3(oldVal.x, oldVal.y, oldVal.z),
                valueNew = new float3(newVal.x, newVal.y, newVal.z),
#if UNITY_EDITOR
                debugOld = oldVal.ToString(),
                debugNew = newVal.ToString()
#endif
            });
        }
    }
}