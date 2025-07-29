using System;
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
        Vector4,
        Quaternion,
        Color,
        String,
        Enum,
        Reference,
        Unknown // Used for system-only reaction entries or editor/non reactor code
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

        public static void LogSystemReaction(string soName, string fieldName, string systemName)
        {
            Add(new()
            {
                frame = Time.frameCount,
                soName = soName,
                fieldName = fieldName,
                systemName = systemName,
                valueType = ReaCSValueKind.Unknown,
#if UNITY_EDITOR
                debugOld = "react",
                debugNew = "react"
#endif
            });
        }


        public static void LogFloat(FixedString64Bytes so, FixedString64Bytes field, float oldVal, float newVal, FixedString64Bytes system)
        {
            Add(new()
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
            Add(new()
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
            Add(new()
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
            Add(new()
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
            Add(new()
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

        public static void LogVector4(FixedString64Bytes so, FixedString64Bytes field, Vector4 oldVal, Vector4 newVal, FixedString64Bytes system)
        {
            Add(new()
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Vector4,
                valueOld = new float3(oldVal.x, oldVal.y, oldVal.z),
                valueNew = new float3(newVal.x, newVal.y, newVal.z),
#if UNITY_EDITOR
                debugOld = oldVal.ToString(),
                debugNew = newVal.ToString()
#endif
            });
        }

        public static void LogQuaternion(FixedString64Bytes so, FixedString64Bytes field, Quaternion oldVal, Quaternion newVal, FixedString64Bytes system)
        {
            Add(new()
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Quaternion,
                valueOld = new float3(oldVal.x, oldVal.y, oldVal.z),
                valueNew = new float3(newVal.x, newVal.y, newVal.z),
#if UNITY_EDITOR
                debugOld = oldVal.eulerAngles.ToString(),
                debugNew = newVal.eulerAngles.ToString()
#endif
            });
        }

        public static void LogColor(FixedString64Bytes so, FixedString64Bytes field, Color oldVal, Color newVal, FixedString64Bytes system)
        {
            Add(new()
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Color,
                valueOld = new float3(oldVal.r, oldVal.g, oldVal.b),
                valueNew = new float3(newVal.r, newVal.g, newVal.b),
#if UNITY_EDITOR
                debugOld = oldVal.ToString(),
                debugNew = newVal.ToString()
#endif
            });
        }

        public static void LogString(FixedString64Bytes so, FixedString64Bytes field, string oldVal, string newVal, FixedString64Bytes system)
        {
            Add(new()
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.String,
#if UNITY_EDITOR
                debugOld = oldVal,
                debugNew = newVal
#endif
            });
        }

        public static void LogEnum(FixedString64Bytes so, FixedString64Bytes field, string oldVal, string newVal, FixedString64Bytes system)
        {
            Add(new()
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Enum,
#if UNITY_EDITOR
                debugOld = oldVal,
                debugNew = newVal
#endif
            });
        }

        public static void LogReference(FixedString64Bytes so, FixedString64Bytes field, string oldName, string newName, FixedString64Bytes system)
        {
            Add(new()
            {
                frame = Time.frameCount,
                soName = so,
                fieldName = field,
                systemName = system,
                valueType = ReaCSValueKind.Reference,
#if UNITY_EDITOR
                debugOld = oldName,
                debugNew = newName
#endif
            });
        }
    }
}
