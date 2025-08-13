using UnityEngine;

namespace ReaCS.Runtime.Core
{
    using UnityEngine;
    using UnityEngine.UIElements;
    using System;

    [Serializable]
    public class StyleTranslateBoxed
    {
        public StyleTranslate Value;

        public StyleTranslateBoxed()
        {
            Value = new StyleTranslate
            {
                value = new Translate(new Length(0f), new Length(0f), 0f),
                keyword = StyleKeyword.Undefined
            };
        }

        public StyleTranslateBoxed(StyleTranslate v)
        {
            Value = v;
        }

        public static implicit operator StyleTranslate(StyleTranslateBoxed b)
            => b?.Value ?? default;

        public static implicit operator StyleTranslateBoxed(StyleTranslate v)
            => new StyleTranslateBoxed(v);
    }

}
