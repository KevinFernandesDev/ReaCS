using System;

namespace ReaCS.Runtime.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ObservableAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class ObservableSavedAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ReactToAttribute : Attribute
    {
        public string FieldName { get; }

        public ReactToAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
