namespace ReaCS.Runtime.Core
{
    public static class Use<T> where T : IReaCSService, new()
    {
        public static readonly T instance = new();
    }
}
