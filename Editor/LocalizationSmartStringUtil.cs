#if UNITY_EDITOR
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
using UnityEditor.Localization;
using System.Collections.Generic;
using System.Linq;

public static class LocalizationSmartStringUtil
{
    public static List<string> GetSmartVariablesForEntry(string table, string entry)
    {
        var vars = new List<string>();
        if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(entry))
            return vars;

        // Find the collection for this table name
        var collection = LocalizationEditorSettings.GetStringTableCollections()
            .FirstOrDefault(col => col.TableCollectionName == table);
        if (collection == null)
            return vars;

        // Get the first StringTable asset (assume default locale for editor)
        var stringTable = collection.StringTables.FirstOrDefault() as StringTable;
        if (stringTable == null)
            return vars;

        // Find the entry by key
        var stringEntry = stringTable.GetEntry(entry);
        if (stringEntry == null || string.IsNullOrEmpty(stringEntry.Value))
            return vars;

        try
        {
            var formatter = LocalizationSettings.StringDatabase.SmartFormatter;
            var errors = new List<string>();
            var parsed = formatter.Parser.ParseFormat(stringEntry.Value, errors);
            CollectPlaceholders(parsed, vars);
        }
        catch
        {
            // Ignore parse errors
        }

        return vars;
    }

    private static void CollectPlaceholders(Format format, List<string> variables)
    {
        foreach (var item in format.Items)
        {
            if (item is Placeholder ph)
            {
                var name = (ph.Selectors != null && ph.Selectors.Count > 0) ? ph.Selectors[0].RawText : null;
                if (!string.IsNullOrEmpty(name) && !variables.Contains(name))
                    variables.Add(name);
            }
            else if (item is Format childFormat)
            {
                CollectPlaceholders(childFormat, variables);
            }
        }
    }
}
#endif
