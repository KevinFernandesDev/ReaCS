#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using ReaCS.Runtime.Internal;
using UnityEditor.Localization;
using UnityEngine.UIElements;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Tables;

namespace ReaCS.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ObservableObject), true)]
    public class ObservableObjectEditor : UnityEditor.Editor
    {
        private List<FieldInfo> observableFields;
        // --- Smart String Editor-only variable storage ---
        private Dictionary<string, string> smartVarValues = new();
        private Dictionary<string, UnityEngine.Object> smartVarObjects = new();

        protected virtual void OnEnable()
        {
            observableFields = new List<FieldInfo>();
            var type = targets.Length > 0
                ? targets[0].GetType()
                : typeof(ObservableObject);

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (Attribute.IsDefined(field, typeof(ObservableAttribute)))
                    observableFields.Add(field);
            }
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("🧹 Invalidate Saved Snapshot"))
            {
                foreach (var obj in targets)
                {
                    if (obj is ObservableObject so)
                    {
                        so.BumpSnapshotVersion();
                        Debug.Log($"[ReaCS] Bumped snapshot version for: {so.name}");
                    }
                }
            }

            serializedObject.Update();

            EditorGUILayout.LabelField("Observables", EditorStyles.boldLabel);

            foreach (var field in observableFields)
            {
                var fieldValues = targets
                    .Select(obj => field.GetValue(obj as ObservableObject))
                    .ToList();

                object fieldValue = fieldValues[0];
                var fieldType = field.FieldType;
                var genericDef = fieldType.IsGenericType ? fieldType.GetGenericTypeDefinition() : null;

                // --- ObservableObjectReference<T> ---
                if (genericDef != null && (genericDef.Name.Contains("ObservableObjectReference")))
                {
                    var valueProp = fieldType.GetProperty("Value");
                    if (valueProp == null)
                    {
                        EditorGUILayout.LabelField(field.Name, "Missing Value property");
                        continue;
                    }

                    object currentValue = valueProp.GetValue(fieldValue);
                    bool mixedValue = !fieldValues.All(v =>
                        Equals(valueProp.GetValue(v), currentValue));

                    EditorGUI.showMixedValue = mixedValue;
                    Type refType = fieldType.GetGenericArguments()[0];
                    object newValue = EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(field.Name), currentValue as ObservableObject, refType, false);
                    EditorGUI.showMixedValue = false;

                    if (!Equals(newValue, currentValue))
                    {
                        foreach (var (obj, fv) in targets.Zip(fieldValues, (o, f) => (o, f)))
                        {
                            valueProp.SetValue(fv, newValue);
                            EditorUtility.SetDirty(obj);
                        }
                    }
                    continue;
                }

                // --- Observable<T> ---
                if (genericDef == typeof(Observable<>))
                {
                    var valueProp = fieldType.GetProperty("Value");
                    var persistField = fieldType.GetField("ShouldPersist", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (valueProp == null)
                    {
                        EditorGUILayout.LabelField(field.Name, "Missing Value property");
                        continue;
                    }

                    object currentValue = valueProp.GetValue(fieldValue);
                    bool mixedValue = !fieldValues.All(v =>
                        Equals(valueProp.GetValue(v), currentValue));

                    EditorGUI.showMixedValue = mixedValue;
                    EditorGUILayout.BeginHorizontal();

                    string label = ObjectNames.NicifyVariableName(field.Name);
                    object newValue = DrawField(label, currentValue, field);
                    EditorGUI.showMixedValue = false;

                    if (!Equals(newValue, currentValue))
                    {
                        foreach (var (obj, fv) in targets.Zip(fieldValues, (o, f) => (o, f)))
                        {
                            valueProp.SetValue(fv, newValue);
                            EditorUtility.SetDirty(obj);
                        }
                    }

                    // ShouldPersist toggle
                    if (persistField != null)
                    {
                        bool shouldPersist = (bool)persistField.GetValue(fieldValue);
                        bool mixedPersist = !fieldValues.All(v =>
                            Equals(persistField.GetValue(v), shouldPersist));
                        EditorGUI.showMixedValue = mixedPersist;

                        bool newShouldPersist = GUILayout.Toggle(shouldPersist, shouldPersist ? "🔒" : "🔄", GUILayout.Width(30));
                        EditorGUI.showMixedValue = false;

                        if (newShouldPersist != shouldPersist)
                        {
                            foreach (var (obj, fv) in targets.Zip(fieldValues, (o, f) => (o, f)))
                            {
                                persistField.SetValue(fv, newShouldPersist);
                                EditorUtility.SetDirty(obj);
                            }
                            AssetDatabase.SaveAssets();
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.LabelField(field.Name, "Unsupported observable type");
            }

            var excluded = new List<string> { "m_Script" };
            foreach (var field in observableFields)
                excluded.Add(field.Name);

            DrawPropertiesExcluding(serializedObject, excluded.ToArray());

            foreach (var obj in targets)
            {
                var so = obj as ObservableObject;
                if (so == null) continue;
                foreach (var field in observableFields)
                {
                    var fieldValue = field.GetValue(so);
                    if (fieldValue == null) continue;
                    var syncMethod = fieldValue.GetType().GetMethod("EditorSyncFromInspector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    syncMethod?.Invoke(fieldValue, null);
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space(12);
            DrawLinkInfo(targets[0] as ObservableObject);
        }

        // Helper function (add at top-level of your editor script)
        private static AddressableAssetSettings GetAddressableSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:AddressableAssetSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
            }
            return null;
        }


        private object DrawField(string label, object value, FieldInfo backingField = null)
        {
            var type = value?.GetType();

            // --- LocaleString Support ---
            if (type == typeof(LocaleString))
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                // --- Unwrap Observable<LocaleString> ---
                var sampleObservable = backingField?.GetValue(targets[0]);
                var sampleLocaleString = sampleObservable?.GetType().GetProperty("Value")?.GetValue(sampleObservable) as LocaleString;
                if (sampleLocaleString == null)
                {
                    EditorGUILayout.HelpBox("LocaleString is null or unreadable.", MessageType.Warning);
                    return value;
                }

                // Table & entry
                string[] tables = GetAllLocalizationTableNames();
                int selectedTable = Mathf.Max(0, Array.IndexOf(tables, sampleLocaleString.table));
                if (selectedTable < 0) selectedTable = 0;
                string table = tables.Length > 0 ? tables[selectedTable] : "";

                string[] entries = GetAllLocalizationEntryKeys(table);
                int selectedEntry = Mathf.Max(0, Array.IndexOf(entries, sampleLocaleString.entry));
                if (selectedEntry < 0) selectedEntry = 0;
                string entry = entries.Length > 0 ? entries[selectedEntry] : "";

                // Table popup
                EditorGUI.BeginChangeCheck();
                selectedTable = EditorGUILayout.Popup("Table", selectedTable, tables);
                if (EditorGUI.EndChangeCheck())
                {
                    table = tables[selectedTable];
                    entries = GetAllLocalizationEntryKeys(table);
                    selectedEntry = 0;
                    entry = entries.Length > 0 ? entries[0] : "";
                }
                else
                {
                    table = tables[selectedTable];
                }

                // Entry popup or text
                if (entries.Length > 0)
                {
                    selectedEntry = EditorGUILayout.Popup("Key", selectedEntry, entries);
                    entry = entries[selectedEntry];
                }
                else
                {
                    entry = EditorGUILayout.TextField("Key", entry);
                }

                // Write table/entry to all selected objects
                foreach (var obj in targets)
                {
                    var observable = backingField?.GetValue(obj);
                    var ls = observable?.GetType().GetProperty("Value")?.GetValue(observable) as LocaleString;
                    if (ls != null)
                    {
                        ls.table = table;
                        ls.entry = entry;
                        EditorUtility.SetDirty(obj);
                    }
                }

                // Smart string vars
                if (!string.IsNullOrEmpty(table) && !string.IsNullOrEmpty(entry))
                {
                    var smartVars = LocalizationSmartStringUtil.GetSmartVariablesForEntry(table, entry);
                    if (smartVars.Count > 0)
                    {
                        EditorGUILayout.HelpBox("Smart String Variables: " + string.Join(", ", smartVars), MessageType.Info);

                        foreach (var varName in smartVars)
                        {
                            EditorGUILayout.BeginHorizontal();

                            string currentVal = sampleLocaleString.Variables.TryGetValue(varName, out var val) ? val?.ToString() ?? "" : "";
                            string newVal = EditorGUILayout.TextField(varName, currentVal, GUILayout.MinWidth(80));

                            if (newVal != currentVal)
                            {
                                foreach (var obj in targets)
                                {
                                    var observable = backingField?.GetValue(obj);
                                    var ls = observable?.GetType().GetProperty("Value")?.GetValue(observable) as LocaleString;
                                    if (ls != null)
                                    {
                                        ls.SetVariable(varName, newVal);
                                        EditorUtility.SetDirty(obj);
                                    }
                                }
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        // --- Preview ---
                        var formatter = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.SmartFormatter;
                        string rawStr = "";
                        var collection = UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollections()
                            .FirstOrDefault(col => col.TableCollectionName == table);
                        var stringTable = collection?.StringTables.FirstOrDefault() as UnityEngine.Localization.Tables.StringTable;
                        var stringEntry = stringTable?.GetEntry(entry);
                        if (stringEntry != null)
                            rawStr = stringEntry.Value;

                        var data = new Dictionary<string, object>(sampleLocaleString.Variables);
                        try
                        {
                            string preview = formatter.Format(rawStr, data);
                            foreach (var obj in targets)
                            {
                                var observable = backingField?.GetValue(obj);
                                var ls = observable?.GetType().GetProperty("Value")?.GetValue(observable) as LocaleString;
                                if (ls != null)
                                    ls.editorPreview = preview;
                            }

                            EditorGUILayout.HelpBox("Preview: " + preview, MessageType.None);
                        }
                        catch
                        {
                            EditorGUILayout.HelpBox("Smart string formatting error", MessageType.Warning);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No Smart String variables detected.", MessageType.None);
                    }
                }

                EditorGUILayout.EndVertical();
                return value;
            }





            // --- Robust StyleEnum<T> support ---
            if (type != null && type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("StyleEnum"))
            {
                var valueProp = type.GetProperty("value");
                if (valueProp == null)
                {
                    EditorGUILayout.LabelField(label, "Unsupported StyleEnum (no 'value')");
                    return value;
                }
                var styleInnerEnum = valueProp.GetValue(value);
                Type enumType = styleInnerEnum?.GetType() ?? type.GetGenericArguments()[0];
                if (!enumType.IsEnum)
                {
                    EditorGUILayout.LabelField(label, "Unsupported StyleEnum (not enum)");
                    return value;
                }

                var newStyleEnum = EditorGUILayout.EnumPopup(label, (Enum)styleInnerEnum);

                if (!Equals(styleInnerEnum, newStyleEnum))
                {
                    object newStruct = Activator.CreateInstance(type);
                    valueProp.SetValue(newStruct, newStyleEnum);
                    return newStruct;
                }
                return value;
            }

            // --- StyleFloat, StyleInt, StyleColor, etc. ---
            if (type != null && type.FullName.StartsWith("UnityEngine.UIElements.Style") && type.FullName != "UnityEngine.UIElements.StyleLength")
            {
                var valueProp = type.GetProperty("value");
                var keywordProp = type.GetProperty("keyword"); // May be null

                if (valueProp == null)
                {
                    EditorGUILayout.LabelField(label, "Unsupported Style type (no 'value')");
                    return value;
                }

                var styleInnerVal = valueProp.GetValue(value);
                object newInnerVal = styleInnerVal;

                if (styleInnerVal is Color col)
                    newInnerVal = EditorGUILayout.ColorField(label, col);
                else if (styleInnerVal is float f)
                    newInnerVal = EditorGUILayout.FloatField(label, f);
                else if (styleInnerVal is int i)
                    newInnerVal = EditorGUILayout.IntField(label, i);
                else if (styleInnerVal is Enum styleEnumFallback)
                    newInnerVal = EditorGUILayout.EnumPopup(label, styleEnumFallback);
                else
                    EditorGUILayout.LabelField(label, $"Unsupported style value: {styleInnerVal?.GetType().Name}");

                if (!Equals(newInnerVal, styleInnerVal))
                {
                    object newStruct = Activator.CreateInstance(type);
                    valueProp.SetValue(newStruct, newInnerVal);
                    if (keywordProp != null)
                        keywordProp.SetValue(newStruct, keywordProp.GetValue(value));
                    return newStruct;
                }
                return value;
            }

            // --- StyleLengthBoxed Support ---
            if (type != null && type == typeof(StyleLengthBoxed))
            {
                var boxed = (StyleLengthBoxed)value ?? new StyleLengthBoxed();
                var styleLength = boxed.Value;

                Length length = styleLength.keyword == StyleKeyword.Undefined
                    ? styleLength.value
                    : new Length(0f, LengthUnit.Pixel);

                float floatVal = length.value;
                LengthUnit unitVal = length.unit;

                EditorGUILayout.BeginHorizontal();
                float newFloat = EditorGUILayout.FloatField(label, floatVal);
                var newUnit = (LengthUnit)EditorGUILayout.EnumPopup(unitVal, GUILayout.MaxWidth(70));
                EditorGUILayout.EndHorizontal();

                var newKeyword = (StyleKeyword)EditorGUILayout.EnumPopup("Keyword", styleLength.keyword);

                bool changed = newFloat != floatVal || newUnit != unitVal || newKeyword != styleLength.keyword;

                if (changed)
                {
                    var newLength = new Length(newFloat, newUnit);
                    var newStyleLength = newKeyword == StyleKeyword.Undefined
                        ? new StyleLength(newLength)
                        : new StyleLength(newKeyword);
                    return new StyleLengthBoxed(newStyleLength);
                }
                return value;
            }

            // --- StyleTranslateBoxed Support ---
            if (type == typeof(StyleTranslateBoxed))
            {
                var boxed = (StyleTranslateBoxed)value ?? new StyleTranslateBoxed();

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                // Position
                boxed.x.Value = EditorGUILayout.FloatField("X", boxed.x.Value);
                boxed.unitX.Value = (LengthUnit)EditorGUILayout.EnumPopup("X Unit", boxed.unitX.Value);

                boxed.y.Value = EditorGUILayout.FloatField("Y", boxed.y.Value);
                boxed.unitY.Value = (LengthUnit)EditorGUILayout.EnumPopup("Y Unit", boxed.unitY.Value);

                // Z + keyword
                boxed.z.Value = EditorGUILayout.FloatField("Z", boxed.z.Value);
                boxed.keyword.Value = (StyleKeyword)EditorGUILayout.EnumPopup("Keyword", boxed.keyword.Value);

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();

                return boxed;
            }




            // --- AssetReference Support ---
            // --- AssetReference Support ---
            if (value is AssetReference assetRef)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));

                var settings = GetAddressableSettings();
                if (settings != null)
                {
                    string assetGUID = assetRef.AssetGUID;
                    string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                    Type assetType = typeof(UnityEngine.Object);

                    // Handle specific AssetReference types (built-in Unity types)
                    if (assetRef is AssetReferenceGameObject) assetType = typeof(GameObject);
                    else if (assetRef is AssetReferenceSprite) assetType = typeof(Sprite);
                    else if (assetRef is AssetReferenceTexture) assetType = typeof(Texture);
                    else if (assetRef is AssetReferenceTexture2D) assetType = typeof(Texture2D);

                    UnityEngine.Object loadedAsset = !string.IsNullOrEmpty(assetPath)
                        ? AssetDatabase.LoadAssetAtPath(assetPath, assetType)
                        : null;

                    UnityEngine.Object newAsset = EditorGUILayout.ObjectField(loadedAsset, assetType, false);

                    if (newAsset != loadedAsset)
                    {
                        string newPath = AssetDatabase.GetAssetPath(newAsset);
                        string newGUID = AssetDatabase.AssetPathToGUID(newPath);
                        assetRef.SetEditorAsset(newAsset);
                        EditorUtility.SetDirty(target);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Addressable settings not found", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndHorizontal();
                return assetRef;
            }



            // --- ObjectField support for Observable<T> where T : UnityEngine.Object ---
            if (value is UnityEngine.Object objVal)
            {
                Type expectedType = typeof(UnityEngine.Object);
                if (backingField != null && backingField.FieldType.IsGenericType)
                {
                    var genericArg = backingField.FieldType.GetGenericArguments().FirstOrDefault();
                    if (genericArg != null && typeof(UnityEngine.Object).IsAssignableFrom(genericArg))
                        expectedType = genericArg;
                }
                return EditorGUILayout.ObjectField(label, objVal, expectedType, false);
            }
            if (value is Sprite spriteVal)
                return EditorGUILayout.ObjectField(label, spriteVal, typeof(Sprite), false);
            if (value is GameObject goVal)
                return EditorGUILayout.ObjectField(label, goVal, typeof(GameObject), true);
            if (value is AudioClip audioVal)
                return EditorGUILayout.ObjectField(label, audioVal, typeof(AudioClip), false);

            // --- Standard primitives ---
            if (value is float rawFloatVal)
            {
                var range = backingField?.GetCustomAttribute<ObservableRangeAttribute>();
                return range != null
                    ? EditorGUILayout.Slider(label, rawFloatVal, range.min, range.max)
                    : EditorGUILayout.FloatField(label, rawFloatVal);
            }
            if (value is int intVal)
                return EditorGUILayout.IntField(label, intVal);
            if (value is bool boolVal)
                return EditorGUILayout.Toggle(label, boolVal);

            if ((value is string || (value == null && backingField != null && backingField.FieldType == typeof(string))))
            {
                string strVal = value as string ?? "";
                return EditorGUILayout.TextField(label, strVal);
            }

            if (value is Enum baseEnumVal)
                return EditorGUILayout.EnumPopup(label, baseEnumVal);
            if (value is Vector2 vec2Val)
                return EditorGUILayout.Vector2Field(label, vec2Val);
            if (value is Vector3 vec3Val)
                return EditorGUILayout.Vector3Field(label, vec3Val);
            if (value is Vector4 vec4Val)
                return EditorGUILayout.Vector4Field(label, vec4Val);
            if (value is Quaternion quatVal)
            {
                Vector4 raw = new(quatVal.x, quatVal.y, quatVal.z, quatVal.w);
                Vector4 newVal = EditorGUILayout.Vector4Field(label + " (xyzw)", raw);
                return new Quaternion(newVal.x, newVal.y, newVal.z, newVal.w);
            }
            if (value is Color colorVal)
                return EditorGUILayout.ColorField(label, colorVal);

            EditorGUILayout.LabelField(label, "Unsupported type");
            return value;
        }

        private void DrawLinkInfo(ObservableObject observableObject)
        {
            if (observableObject == null) return;
            var linkCount = ReaCS.Runtime.Access.Query<LinkRegistry>().CountLinksFor(observableObject);

            EditorGUILayout.LabelField("Link Tree", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"🔗 {linkCount} links found", EditorStyles.helpBox);
        }

        // --- Localization helpers (Unity Localization package required) ---
        private static string[] GetAllLocalizationTableNames()
        {
#if UNITY_EDITOR
            // Get all string table collections
            return LocalizationEditorSettings.GetStringTableCollections()
                .Select(col => col.TableCollectionName)
                .ToArray();
#else
            return new[] { "Default" };
#endif
        }

        private static string[] GetAllLocalizationEntryKeys(string table)
        {
#if UNITY_EDITOR
            var collection = LocalizationEditorSettings.GetStringTableCollections()
                .FirstOrDefault(col => col.TableCollectionName == table);
            if (collection == null) return new string[0];
            return collection.SharedData.Entries.Select(e => e.Key).ToArray();
#else
            return new string[0];
#endif
        }
    }
}
#endif
