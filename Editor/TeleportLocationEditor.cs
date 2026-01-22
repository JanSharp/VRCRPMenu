using System.Collections.Generic;
using System.Linq;
using JanSharp.Internal;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace JanSharp
{
    [InitializeOnLoad]
    public static class TeleportLocationOnBuild
    {
        static TeleportLocationOnBuild()
        {
            OnBuildUtil.RegisterTypeCumulative<TeleportLocation>(OnBuildCumulative);
        }

        private static bool OnBuildCumulative(IEnumerable<TeleportLocation> teleportLocations)
        {
            bool result = true;
            foreach (var teleportLocation in teleportLocations)
            {
                if (!PermissionSystemEditorUtil.OnPermissionConditionsListBuild(
                    teleportLocation,
                    teleportLocation.AssetGuids,
                    permissionDefsFieldName: "permissionDefs",
                    conditionsHeaderName: "Conditions"))
                {
                    result = false;
                }
            }
            return result;
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(TeleportLocation))]
    public class TeleportLocationEditor : Editor
    {
        private SerializedProperty displayNameProp;
        private SerializedProperty categoryNameProp;
        private SerializedProperty whenConditionsAreMetProp;
        private PermissionConditionsList conditionsList;
        private TeleportLocationsEditorData data;

        private TeleportLocation[] GetAllLocations()
            => FindObjectsByType<TeleportLocation>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        private TeleportLocationsManager GetManager()
            => FindObjectsByType<TeleportLocationsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(m => !EditorUtil.IsEditorOnly(m));

        public void OnEnable()
        {
            displayNameProp = serializedObject.FindProperty("displayName");
            categoryNameProp = serializedObject.FindProperty("categoryName");
            whenConditionsAreMetProp = serializedObject.FindProperty("whenConditionsAreMet");

            conditionsList = new PermissionConditionsList(
                targets: targets,
                header: new GUIContent("Conditions"),
                logicalAndsFieldName: "logicalAnds",
                invertsFieldName: "inverts",
                assetGuidsFieldName: "assetGuids",
                getLogicalAnds: t => ((TeleportLocation)t).logicalAnds,
                getInverts: t => ((TeleportLocation)t).inverts,
                getAssetGuids: t => ((TeleportLocation)t).AssetGuids);

            TryBuildEditorData();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void TryBuildEditorData()
        {
            TeleportLocationsManager manager = GetManager();
            if (manager == null)
                return;
            data = new(manager);
            data.CollectAllData(GetAllLocations());
            data.CreateInspectorLists();
        }

        private void ManagerExistenceMayHaveChanged()
        {
            if (data == null)
                TryBuildEditorData();
            else if (data.manager == null)
            {
                data.DestroyInspectorLists();
                data = null;
            }
            else
                data.CollectAllData(GetAllLocations());
            Repaint();
        }

        public void OnDisable()
        {
            conditionsList.OnDisable();
        }

        private void OnUndoRedo()
        {
            ManagerExistenceMayHaveChanged();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;

            bool categoryNameChanged;

            serializedObject.Update();
            EditorGUILayout.PropertyField(displayNameProp);
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                if (data != null)
                    DrawSelectorField(categoryNameProp, data.categoryNames.ToArray());
                else
                    EditorGUILayout.PropertyField(categoryNameProp);
                categoryNameChanged = scope.changed;
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(whenConditionsAreMetProp);
            serializedObject.ApplyModifiedProperties();

            if (categoryNameChanged && data != null)
            {
                data.CollectAllData(data.allLocations);
                Repaint();
                return;
            }

            conditionsList.Draw();
            EditorGUILayout.Space();

            if (data != null)
                data.Draw();
            else
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label("In order to set the order of teleport locations the manager is required. "
                        + "Running On Build handlers will create the manager (so long as there is at least one "
                        + "non editor only location in the scene).", EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("Run On Build Handlers"))
                    {
                        OnBuildUtil.RunOnBuild(showDialogOnFailure: true, useSceneViewNotification: false, abortIfScriptsGotInstantiated: false);
                        ManagerExistenceMayHaveChanged();
                    }
                }
        }

        private static void DrawSelectorField(SerializedProperty prop, string[] names)
        {
            using (new GUILayout.HorizontalScope())
            {
                int currentIndex = prop.hasMultipleDifferentValues ? -1 : System.Array.IndexOf(names, prop.stringValue);
                EditorGUILayout.PropertyField(prop);
                int index = EditorGUILayout.Popup(currentIndex, names, GUILayout.Width(20f));
                if (index != currentIndex)
                    prop.stringValue = names[index];
            }
        }
    }
}
