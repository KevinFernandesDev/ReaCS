using System;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    /// <summary>
    /// Lightweight, Burst-safe unique identifier for logical entities (e.g., a group of components on a GameObject).
    /// Assigned at runtime via SharedEntityIdService.
    /// </summary>
    [Serializable]
    public struct EntityId : IEquatable<EntityId>
    {
        [SerializeField] private int value;

        public int Value => value;
        public static readonly EntityId None = new EntityId(0);

        public EntityId(int id) => value = id;

        public static implicit operator int(EntityId id) => id.value;
        public static implicit operator EntityId(int id) => new EntityId(id);

        public bool Equals(EntityId other) => value == other.value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => value;
        public override string ToString() => $"EntityId({value})";

        public static bool operator ==(EntityId a, EntityId b) => a.value == b.value;
        public static bool operator !=(EntityId a, EntityId b) => a.value != b.value;
    }
}
