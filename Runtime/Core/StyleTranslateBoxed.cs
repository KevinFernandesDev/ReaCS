using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class StyleTranslateBoxed
    {
        [Observable] public Observable<float> x = new();
        [Observable] public Observable<float> y = new();
        [Observable] public Observable<float> z = new();

        [Observable] public Observable<LengthUnit> unitX = new() { Value = LengthUnit.Pixel };
        [Observable] public Observable<LengthUnit> unitY = new() { Value = LengthUnit.Pixel };

        [Observable] public Observable<StyleKeyword> keyword = new() { Value = StyleKeyword.Undefined };

        public StyleTranslate ToStyleTranslate()
        {
            return new StyleTranslate
            {
                value = new Translate(
                    new Length(x.Value, unitX.Value),
                    new Length(y.Value, unitY.Value),
                    z.Value
                ),
                keyword = keyword.Value
            };
        }

        public void FromStyleTranslate(StyleTranslate styleTranslate)
        {
            x.Value = styleTranslate.value.x.value;
            y.Value = styleTranslate.value.y.value;
            z.Value = styleTranslate.value.z;

            unitX.Value = styleTranslate.value.x.unit;
            unitY.Value = styleTranslate.value.y.unit;
            keyword.Value = styleTranslate.keyword;
        }

        public static implicit operator StyleTranslate(StyleTranslateBoxed b) => b?.ToStyleTranslate() ?? default;
        public static implicit operator StyleTranslateBoxed(StyleTranslate v)
        {
            var b = new StyleTranslateBoxed();
            b.FromStyleTranslate(v);
            return b;
        }
    }
}
