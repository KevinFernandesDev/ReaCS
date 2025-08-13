using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class StyleTranslateBoxed
    {
        public Observable<StyleLength> x = new();
        public Observable<StyleLength> y = new();
        public Observable<float> z = new();
        public Observable<StyleKeyword> keyword = new();

        [field: SerializeField]
        [field: CreateProperty] // This makes it bindable in UI Toolkit
        public StyleTranslate Value
        {
            get => new StyleTranslate
            {
                value = new Translate(x.Value.value, y.Value.value, z.Value),
                keyword = keyword.Value
            };
            set
            {
                x.Value = new StyleLength(value.value.x);
                y.Value = new StyleLength(value.value.y);
                z.Value = value.value.z;
                keyword.Value = value.keyword;
            }
        }
    }
}
