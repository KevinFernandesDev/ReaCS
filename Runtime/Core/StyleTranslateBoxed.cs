using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class StyleTranslateBoxed
    {
        public float x = 0f;
        public float y = 0f;
        public float z = 0f;
        public LengthUnit unitX = LengthUnit.Pixel;
        public LengthUnit unitY = LengthUnit.Pixel;
        public StyleKeyword keyword = StyleKeyword.Undefined;

        public StyleTranslate ToStyleTranslate()
        {
            return new StyleTranslate
            {
                value = new Translate(
                    new Length(x, unitX),
                    new Length(y, unitY),
                    z
                ),
                keyword = keyword
            };
        }

        public void FromStyleTranslate(StyleTranslate styleTranslate)
        {
            x = styleTranslate.value.x.value;
            y = styleTranslate.value.y.value;
            z = styleTranslate.value.z;
            unitX = styleTranslate.value.x.unit;
            unitY = styleTranslate.value.y.unit;
            keyword = styleTranslate.keyword;
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
