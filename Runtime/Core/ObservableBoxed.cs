using System;

[Serializable]
public class ObservableBoxed<T>
{
    public T Value;

    public ObservableBoxed() { }

    public ObservableBoxed(T value)
    {
        Value = value;
    }
}
