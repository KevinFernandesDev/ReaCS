#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Localization;
using System.Linq;

public class TestLocalizationMenu
{
    [MenuItem("Tools/Localization/Print Tables")]
    public static void PrintTables()
    {
        var tableNames = LocalizationEditorSettings.GetStringTableCollections().ToArray();
        foreach (var name in tableNames)
            Debug.Log("Table: " + name);
    }
}
#endif
