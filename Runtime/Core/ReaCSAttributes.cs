using System;
using UnityEngine;

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

    public class ObservableRangeAttribute : PropertyAttribute
    {
        public float min, max;
        public ObservableRangeAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
