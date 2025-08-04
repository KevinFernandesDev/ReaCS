using ReaCS.Runtime.Core;
using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace ReaCS.Runtime.Internal
{
    public static class HandleStringKeyToLocalizedStringConversion
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void RegisterConverters()
        {
            var group = new ConverterGroup("LocalizeStringKey");
            group.AddConverter((ref string value) =>
            {
                var tableName = string.IsNullOrEmpty(LocalizationSettings.SelectedLocale?.Identifier.Code)
                    ? "Default"
                    : LocalizationSettings.ProjectLocale?.Identifier.Code ?? "Default";
                var localizedString = new LocalizedString(tableName, value);
                return localizedString.GetLocalizedString();
            });

            group.AddConverter((ref LocaleString locale) =>
            {
                var localizedString = new LocalizedString(locale.table, locale.entry);
                return localizedString.GetLocalizedString();
            });
            ConverterGroups.RegisterConverterGroup(group);
        }
    }
}