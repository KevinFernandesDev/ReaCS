using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Localization;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class LocaleString : INotifyPropertyChanged, ISerializationCallbackReceiver
    {
        public string table;
        public string entry;

#if UNITY_EDITOR
        [NonSerialized]
        public string editorPreview; // Used for UI Toolkit preview in Editor
#endif

        [Serializable]
        private struct SmartVarEntry
        {
            public string key;
            public string textValue;
        }

        [SerializeField]
        private List<SmartVarEntry> _serializedVars = new();

        [NonSerialized]
        private Dictionary<string, object> _variables = new();

        public IReadOnlyDictionary<string, object> Variables => _variables;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public void SetVariable(string key, object val)
        {
            if (!_variables.TryGetValue(key, out var old) || !Equals(old, val))
            {
                _variables[key] = val;
#if UNITY_EDITOR
                editorPreview = Value;
#endif
                OnPropertyChanged(nameof(Value));
            }
        }

        public void ClearVariables()
        {
            _variables.Clear();
#if UNITY_EDITOR
            editorPreview = Value;
#endif
            OnPropertyChanged(nameof(Value));
        }

        public LocalizedString ToLocalizedString()
        {
            var ls = new LocalizedString(table, entry);
            if (_variables.Count > 0)
                ls.Arguments = new object[] { DictionaryWrapper.For(_variables) };
            return ls;
        }

        public string Value
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying && !string.IsNullOrEmpty(editorPreview))
                    return editorPreview;
#endif
                var ls = new LocalizedString(table, entry);
                if (_variables.Count > 0)
                    ls.Arguments = new object[] { DictionaryWrapper.For(_variables) };
                return ls.GetLocalizedString();
            }
        }

        public override string ToString() => Value;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _serializedVars.Clear();
            foreach (var kv in _variables)
            {
                _serializedVars.Add(new SmartVarEntry
                {
                    key = kv.Key,
                    textValue = kv.Value?.ToString() ?? ""
                });
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _variables = new Dictionary<string, object>();
            foreach (var entry in _serializedVars)
            {
                _variables[entry.key] = entry.textValue;
            }
        }

        [Serializable]
        private class DictionaryWrapper
        {
            private readonly Dictionary<string, object> _dict;
            private DictionaryWrapper(Dictionary<string, object> dict) => _dict = dict;
            public static DictionaryWrapper For(Dictionary<string, object> dict) => new DictionaryWrapper(dict);
            public object this[string key] => _dict.TryGetValue(key, out var val) ? val : null;
        }
    }
}
