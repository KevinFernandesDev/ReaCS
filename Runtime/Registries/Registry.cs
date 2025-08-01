using ReaCS.Runtime.Core;
using System.Collections.Generic;

public abstract class Registry<T> : IReaCSQuery
{
    protected readonly HashSet<T> _registered = new();

    public virtual void Register(T obj)
    {

    }

    public virtual void Unregister(T obj)
    {

    }

    public virtual void Clear() => _registered.Clear();

    public virtual bool Contains(T obj) => _registered.Contains(obj);

    public virtual IEnumerable<T> GetAll() => _registered;
}