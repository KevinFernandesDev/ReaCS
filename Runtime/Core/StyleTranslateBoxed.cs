using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class StyleTranslateBoxed
    {
        public Vector2 position = Vector2.zero;
        public float z = 0f;

        public LengthUnit unitX = LengthUnit.Pixel;
        public LengthUnit unitY = LengthUnit.Pixel;

        public StyleKeyword keyword = StyleKeyword.Undefined;

        public StyleTranslate ToStyleTranslate()
        {
            return new StyleTranslate
            {
                value = new Translate(
                    new Length(position.x, unitX),
                    new Length(position.y, unitY),
                    z
                ),
                keyword = keyword
            };
        }

        public void FromStyleTranslate(StyleTranslate styleTranslate)
        {
            position = new Vector2(
                styleTranslate.value.x.value,
                styleTranslate.value.y.value
            );
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
