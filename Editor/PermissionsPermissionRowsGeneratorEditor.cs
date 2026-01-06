using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JanSharp
{
    [InitializeOnLoad]
    public static class PermissionsPermissionRowsGeneratorOnBuild
    {
        private static PermissionDefinition[] allPermissionDefs;

        static PermissionsPermissionRowsGeneratorOnBuild()
        {
            OnBuildUtil.RegisterType<PermissionManagerAPI>(OnPermissionManagerBuild, order: -22);
            OnBuildUtil.RegisterType<PermissionsPermissionRowsGenerator>(OnBuild, order: -21);
        }

        private static bool OnPermissionManagerBuild(PermissionManagerAPI permissionManager)
        {
            allPermissionDefs = permissionManager?.PermissionDefinitions ?? new PermissionDefinition[0];
            return true;
        }

        private static bool OnBuild(PermissionsPermissionRowsGenerator generator)
        {
            PermissionsPermissionRow[] existingRows = generator.content.GetComponentsInChildren<PermissionsPermissionRow>(includeInactive: true);
            List<RectTransform> customElements = generator.content
                .Cast<RectTransform>()
                .Except(existingRows.Select(r => (RectTransform)r.transform))
                .ToList();
            for (int i = allPermissionDefs.Length; i < existingRows.Length; i++)
                Undo.DestroyObjectImmediate(existingRows[i].gameObject);

            VerticalLayoutGroup layoutGroup = generator.layoutGroupSpecification;
            float currentY = layoutGroup.padding.top;
            for (int i = 0; i < customElements.Count; i++)
            {
                if (i != 0)
                    currentY += layoutGroup.spacing;
                SetContentElementPosition(customElements[i], layoutGroup, ref currentY);
            }

            if (customElements.Count != 0 && allPermissionDefs.Length != 0)
                currentY += layoutGroup.spacing;

            PermissionsPermissionRow[] rows = existingRows.Length == allPermissionDefs.Length
                ? existingRows
                : new PermissionsPermissionRow[allPermissionDefs.Length];
            for (int i = 0; i < allPermissionDefs.Length; i++)
            {
                PermissionsPermissionRow row = i < existingRows.Length
                    ? existingRows[i]
                    : CreateNewRow(generator);
                if (i != 0)
                    currentY += layoutGroup.spacing;
                SetContentElementPosition((RectTransform)row.transform, layoutGroup, ref currentY);
                SetLabelText(row, allPermissionDefs[i]);
                SetPermissionDefRef(row, allPermissionDefs[i]);
                UnsetEditorOnlyTag(row.gameObject, generator.permissionRowTagToUse);
                rows[i] = row;
            }

            SetPermissionDefsOnPage(generator.page, rows);
            SetContentSize(generator, currentY + layoutGroup.padding.bottom);

            return true;
        }

        private static PermissionsPermissionRow CreateNewRow(PermissionsPermissionRowsGenerator generator)
        {
            GameObject go = Object.Instantiate(generator.permissionRowPrefab);
            go.transform.SetParent(generator.content, worldPositionStays: false);
            Undo.RegisterCreatedObjectUndo(go, "Generate Permissions Permission Row");
            OnBuildUtil.MarkForRerunDueToScriptInstantiation();
            return go.GetComponent<PermissionsPermissionRow>();
        }

        private static void SetContentElementPosition(RectTransform rect, VerticalLayoutGroup layoutGroup, ref float yPosition)
        {
            SerializedObject so = new(rect);
            Vector2 sizeDelta = rect.sizeDelta;
            sizeDelta.x = -(layoutGroup.padding.left + layoutGroup.padding.right);
            so.FindProperty("m_AnchoredPosition").vector2Value = new Vector2(layoutGroup.padding.left, -yPosition);
            so.FindProperty("m_AnchorMin").vector2Value = new Vector2(0f, 1f);
            so.FindProperty("m_AnchorMax").vector2Value = new Vector2(1f, 1f);
            so.FindProperty("m_Pivot").vector2Value = new Vector2(0f, 1f);
            so.FindProperty("m_SizeDelta").vector2Value = sizeDelta;
            yPosition += sizeDelta.y;
            so.ApplyModifiedProperties();
        }

        private static void SetLabelText(PermissionsPermissionRow row, PermissionDefinition permissionDef)
        {
            SerializedObject so = new(row.label);
            so.FindProperty("m_text").stringValue = permissionDef.displayName;
            so.ApplyModifiedProperties();
        }

        private static void SetPermissionDefRef(PermissionsPermissionRow row, PermissionDefinition permissionDef)
        {
            SerializedObject so = new(row);
            so.FindProperty(nameof(PermissionsPermissionRow.permissionDef)).objectReferenceValue = permissionDef;
            so.ApplyModifiedProperties();
        }

        private static void SetPermissionDefsOnPage(PermissionsPage page, PermissionsPermissionRow[] rows)
        {
            SerializedObject so = new(page);
            EditorUtil.SetArrayProperty(
                so.FindProperty(nameof(PermissionsPage.permissionRows)),
                rows,
                (p, v) => p.objectReferenceValue = v);
            so.ApplyModifiedProperties();
        }

        private static void SetContentSize(PermissionsPermissionRowsGenerator generator, float currentY)
        {
            Vector2 sizeDelta = generator.content.sizeDelta;
            sizeDelta.y = currentY;
            SerializedObject so = new(generator.content);
            so.FindProperty("m_SizeDelta").vector2Value = sizeDelta;
            so.ApplyModifiedProperties();
        }

        private static void UnsetEditorOnlyTag(GameObject toOverwrite, GameObject tagToMatch)
        {
            SerializedObject so = new(toOverwrite);
            so.FindProperty("m_TagString").stringValue = tagToMatch.tag;
            so.ApplyModifiedProperties();
        }
    }
}
