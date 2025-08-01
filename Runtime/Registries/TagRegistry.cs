using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReaCS.Runtime.Registries
{
    public class TagRegistry : Registry<Tag>
    {
        private readonly Dictionary<Type, HashSet<Tag>> _tagsByType = new();

        public override void Register(Tag tag)
        {
            base.Register(tag);

            var type = tag.GetType();
            if (!_tagsByType.TryGetValue(type, out var set))
                _tagsByType[type] = set = new HashSet<Tag>();
            set.Add(tag);
        }

        public override void Unregister(Tag tag)
        {
            base.Unregister(tag);

            var type = tag.GetType();
            if (_tagsByType.TryGetValue(type, out var set))
                set.Remove(tag);
        }

        /// <summary>
        /// O(1) exact-type query (does not return derived).
        /// </summary>
        public IEnumerable<TTag> GetAllOfType<TTag>() where TTag : Tag
            => _tagsByType.TryGetValue(typeof(TTag), out var set)
                ? set.Cast<TTag>()
                : Enumerable.Empty<TTag>();

        /// <summary>
        /// O(N) inheritance query (includes base types).
        /// </summary>
        public IEnumerable<TTag> GetAllAssignableTo<TTag>() where TTag : Tag
        {
            var targetType = typeof(TTag);

            // Fast path: exact matches
            if (_tagsByType.TryGetValue(targetType, out var set))
                foreach (var tag in set)
                    yield return (TTag)tag;

            // Inheritance path: all other assignable subclasses
            foreach (var (keyType, tagSet) in _tagsByType)
            {
                if (keyType == targetType) continue;
                if (targetType.IsAssignableFrom(keyType))
                    foreach (var tag in tagSet)
                        yield return (TTag)tag;
            }
        }
    }
}
