using ReaCS.Runtime.Core;
using UnityEngine;

namespace ReaCS.Runtime.Internal
{
    /// <summary>
    /// Optional base class for tracked observables. Used for callback notification.
    /// </summary>
    public abstract class ObservableBase : IHasFastHash
    {
        public abstract int FastHashValue { get; }
        public abstract void NotifyChanged();
    }
}
