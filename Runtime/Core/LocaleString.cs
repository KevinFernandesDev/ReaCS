#if UNITY_EDITOR
using System;
using UnityEngine.Localization;
#endif

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public struct LocaleString
    {
        public string table;
        public string entry;

        public LocalizedString ToLocalizedString()
        {
            return new LocalizedString(table, entry);
        }

        public override string ToString() => $"{table}::{entry}";
    }
}