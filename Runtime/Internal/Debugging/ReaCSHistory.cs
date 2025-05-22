using System.Collections.Generic;
using UnityEngine;

namespace ReaCS.Runtime.Internal.Debugging
{
    public static class ReaCSHistory
    {
        public static bool Enabled = true;
        public static readonly List<HistoryEntry> Entries = new();
        private const int MaxEntries = 500;

        public static void Log(ScriptableObject so, string fieldName, object oldVal, object newVal, string systemName)
        {
            if (!Enabled || so == null) return;

            Entries.Add(new HistoryEntry
            {
                frame = Time.frameCount,
                soName = so.name,
                fieldName = fieldName,
                oldValue = oldVal?.ToString() ?? "null",
                newValue = newVal?.ToString() ?? "null",
                systemName = systemName
            });

            if (Entries.Count > MaxEntries)
                Entries.RemoveAt(0);
        }

        public static void Clear() => Entries.Clear();
    }
}
