using System;
using UnityEngine;

namespace ReaCS.Runtime.Internal
{
    // ReaCS.Runtime.Internal.Debugging
    [Serializable]
    public struct HistoryEntry
    {
        public int frame;
        public string soName;
        public string fieldName;
        public string oldValue;
        public string newValue;
        public string systemName;
    }
}