#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class AssetReferenceEditorExtensions
{
    /// <summary>
    /// Sets the AssetReference's GUID via a SerializedProperty on its parent.
    /// </summary>
    public static void SetAssetReferenceGUID(SerializedObject parentSO, string propertyPath, string newGuid)
    {
        var prop = parentSO.FindProperty(propertyPath);
        if (prop == null) return;

        var guidProp = prop.FindPropertyRelative("m_AssetGUID");
        if (guidProp != null)
        {
            guidProp.stringValue = newGuid ?? "";
            parentSO.ApplyModifiedProperties();
        }
    }
}
#endif
