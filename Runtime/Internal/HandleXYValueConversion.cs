using UnityEngine;
using UnityEngine.UIElements;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Internal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReaCS.Editor.Internal
{
    public static class HandleXYValueConversion
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void Register()
        {
            var group = new ConverterGroup("XYValue");

            group.AddConverter((ref XYValue data) =>
            {
                return new StyleTranslate(new Translate(
                    new Length(data.x, LengthUnit.Pixel),
                    new Length(data.y, LengthUnit.Pixel),
                    0f));
            });

            ConverterGroups.RegisterConverterGroup(group);
        }
    }
}
