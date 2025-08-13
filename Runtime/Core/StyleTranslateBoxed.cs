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

        public StyleTranslate ToStyleTranslate()
        {
            return new StyleTranslate
            {
                value = new Translate(x.Value.value, y.Value.value, z.Value),
                keyword = keyword.Value
            };
        }

        public void SetFrom(StyleTranslate translate)
        {
            x.Value = new StyleLength(translate.value.x);
            y.Value = new StyleLength(translate.value.y);
            z.Value = translate.value.z;
            keyword.Value = translate.keyword;
        }
    }
}
