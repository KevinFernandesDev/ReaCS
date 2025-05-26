using ReaCS.Runtime.Core;

namespace ReaCS.Runtime
{ 
    public static class ReaCS
    {
        public static T Query<T>() where T : IReaCSQuery, new() => QueryCache<T>.instance;
        public static T Use<T>() where T : IReaCSService, new() => UseCache<T>.instance;

        private static class QueryCache<T> where T : IReaCSQuery, new()
        {
            public static readonly T instance = new();
        }

        private static class UseCache<T> where T : IReaCSService, new()
        {
            public static readonly T instance = new();
        }
    }
}
