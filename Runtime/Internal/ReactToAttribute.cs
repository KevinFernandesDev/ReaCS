using System;

namespace ReaCS.Runtime.Internal
{
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