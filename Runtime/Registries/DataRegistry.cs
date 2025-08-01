using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ReaCS.Runtime.Registries
{
    /// <summary>
    /// Tracks all runtime Data instances by type.
    /// Supports exact-type and inheritance queries.
    /// </summary>
    public class DataRegistry : Registry<Data>, IDisposable
    {
        // Per-type index (does not include derived types for queries)
        private readonly Dictionary<Type, HashSet<Data>> _dataByType = new();

        /// <summary>
        /// Registers the given Data object.
        /// </summary>
        public override void Register(Data data)
        {
            base.Register(data); // Uses Registry<Data> logic (likely a global HashSet/Data list)

            var type = data.GetType();
            if (!_dataByType.TryGetValue(type, out var set))
                _dataByType[type] = set = new HashSet<Data>();

            set.Add(data); // HashSet prevents duplicates.
        }

        /// <summary>
        /// Unregisters the given Data object.
        /// </summary>
        public override void Unregister(Data data)
        {
            base.Unregister(data);

            var type = data.GetType();
            if (_dataByType.TryGetValue(type, out var set))
                set.Remove(data);
        }

        /// <summary>
        /// Returns all Data instances of *exact* type TData (not derived types).
        /// </summary>
        public IEnumerable<TData> GetAllOfType<TData>() where TData : Data
            => _dataByType.TryGetValue(typeof(TData), out var set)
                ? set.Cast<TData>()
                : Enumerable.Empty<TData>();

        /// <summary>
        /// Returns all Data instances assignable to TData (including derived types).
        /// </summary>
        public IEnumerable<TData> GetAllAssignableTo<TData>() where TData : Data
        {
            var targetType = typeof(TData);

            // Fast path: exact matches
            if (_dataByType.TryGetValue(targetType, out var set))
                foreach (var data in set)
                    yield return (TData)data;

            // Inheritance path: all other assignable subclasses
            foreach (var (keyType, dataSet) in _dataByType)
            {
                if (keyType == targetType) continue;
                if (targetType.IsAssignableFrom(keyType))
                    foreach (var data in dataSet)
                        yield return (TData)data;
            }
        }

        public void Dispose()
        {
            Debug.Log("[DataRegistry] Disposing and clearing link map.");
            _dataByType.Clear();
            Clear();
        }
    }
}
