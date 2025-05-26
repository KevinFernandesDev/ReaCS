using System;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    /// <summary>
    /// Lightweight, Burst-safe unique identifier for logical entities (e.g., a group of components on a GameObject).
    /// Assigned at runtime via SharedEntityIdService.
    /// </summary>
    [Serializable]
    public struct ReaCSEntityId : IEquatable<ReaCSEntityId>
    {
        [SerializeField] private int value;

        public int Value => value;
        public static readonly ReaCSEntityId None = new ReaCSEntityId(0);

        public ReaCSEntityId(int id) => value = id;

        public static implicit operator int(ReaCSEntityId id) => id.value;
        public static implicit operator ReaCSEntityId(int id) => new ReaCSEntityId(id);

        public bool Equals(ReaCSEntityId other) => value == other.value;
        public override bool Equals(object obj) => obj is ReaCSEntityId other && Equals(other);
        public override int GetHashCode() => value;
        public override string ToString() => $"EntityId({value})";

        public static bool operator ==(ReaCSEntityId a, ReaCSEntityId b) => a.value == b.value;
        public static bool operator !=(ReaCSEntityId a, ReaCSEntityId b) => a.value != b.value;
    }
}
