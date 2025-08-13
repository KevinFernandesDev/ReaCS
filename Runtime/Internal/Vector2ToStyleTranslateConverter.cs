using UnityEngine;
using UnityEngine.UIElements;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Internal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReaCS.Editor.Internal
{
    public static class Vector2ToStyleTranslateConverter
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void Register()
        {
            var group = new ConverterGroup("Vector2ToStyleTranslate");

            group.AddConverter((ref Vector2 v) =>
            {
                return new StyleTranslate(new Translate(
                    new Length(v.x, LengthUnit.Pixel),
                    new Length(v.y, LengthUnit.Pixel),
                    0f));
            });

            ConverterGroups.RegisterConverterGroup(group);
        }
    }
}