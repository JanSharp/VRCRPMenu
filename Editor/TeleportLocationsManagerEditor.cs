using System.Collections.Generic;
using System.Linq;
using JanSharp.Internal;
using UdonSharpEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace JanSharp
{
    [InitializeOnLoad]
    public static class TeleportLocationsManagerOnBuild
    {
        private static List<TeleportLocation> allLocations;

        static TeleportLocationsManagerOnBuild()
        {
            OnBuildUtil.RegisterTypeCumulative<TeleportLocation>(OnLocationsBuild, order: -10, includeEditorOnly: true);
            OnBuildUtil.RegisterType<TeleportLocationsManager>(OnBuild, order: -9);
        }

        private static bool OnLocationsBuild(IEnumerable<TeleportLocation> locations)
        {
            allLocations = locations.ToList();
            return true;
        }

        private static bool OnBuild(TeleportLocationsManager manager)
        {
            TeleportLocationsEditorData data = new(manager);
            data.CollectAllData(allLocations);
            data.ApplyChanges();
            int order = 1;
            List<TeleportLocation> nonEditorOnlyLocations = new();
            foreach (TeleportLocation location in manager.AllLocations)
            {
                if (EditorUtil.IsEditorOnly(location))
                    continue;
                nonEditorOnlyLocations.Add(location);
                SerializedObject locationSo = new(location);
                locationSo.FindProperty("order").intValue = order++;
                locationSo.ApplyModifiedProperties();
            }
            SerializedObject so = new(manager);
            EditorUtil.SetArrayProperty(
                so.FindProperty("locations"),
                nonEditorOnlyLocations,
                (p, v) => p.objectReferenceValue = v);
            so.ApplyModifiedProperties();
            return true;
        }
    }

    [CustomEditor(typeof(TeleportLocationsManager))]
    public class TeleportLocationsManagerEditor : Editor
    {
        private TeleportLocationsEditorData data;

        private TeleportLocation[] GetAllLocations()
            => FindObjectsByType<TeleportLocation>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        public void OnEnable()
        {
            data = new((TeleportLocationsManager)target);
            data.CollectAllData(GetAllLocations());
            data.CreateInspectorLists();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDisable()
        {
            data.DestroyInspectorLists();
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            data.CollectAllData(GetAllLocations());
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;
            data.Draw();
        }
    }

    internal class TeleportLocationsEditorData
    {
        public TeleportLocationsManager manager;
        public SerializedObject managerSo;
        public List<TeleportLocation> allLocations = new();
        public List<TeleportLocationsCategory> categories = new();
        public List<string> categoryNames = new();
        public Dictionary<string, TeleportLocationsCategory> categoriesByName;
        public TeleportLocationsCategoriesEditorList categoriesList;

        public TeleportLocationsEditorData(TeleportLocationsManager manager)
        {
            this.manager = manager;
            managerSo = new(manager);
        }

        public void CollectAllData(IEnumerable<TeleportLocation> allLocations)
        {
            TeleportLocation[] knownLocations = manager.AllLocations;
            // Cannot rely on the order in which category names appear in the locations list, as that could
            // perceivably randomly change the order of categories when a location's category got changed.
            string[] knownCategoryNames = manager.CategoryNames;

            foreach (TeleportLocationsCategory category in categories)
                category.list?.OnDisable();

            categoriesByName = knownLocations
                .Where(l => l != null)
                .Distinct()
                .GroupBy(l => (l.CategoryName ?? "").Trim())
                .Select(g => new TeleportLocationsCategory(g.Key, g.ToList()))
                .ToDictionary(c => c.categoryName, c => c);
            categories.Clear();
            categoryNames.Clear();
            foreach (string knownName in knownCategoryNames)
            {
                if (!categoriesByName.TryGetValue(knownName, out TeleportLocationsCategory category))
                    continue;
                categories.Add(category);
                categoryNames.Add(knownName);
            }
            HashSet<string> knownCategoryNamesLut = new(knownCategoryNames);
            foreach (var kvp in categoriesByName.Where(kvp => !knownCategoryNames.Contains(kvp.Key)))
            {
                categories.Add(kvp.Value);
                categoryNames.Add(kvp.Key);
            }

            HashSet<TeleportLocation> knownLocationsLut = categories.SelectMany(c => c.locations).ToHashSet();
            foreach (TeleportLocation location in allLocations)
            {
                if (knownLocationsLut.Contains(location))
                    continue;
                string categoryName = (location.CategoryName ?? "").Trim();
                if (categoriesByName.TryGetValue(categoryName, out TeleportLocationsCategory category))
                    category.locations.Add(location);
                else
                {
                    category = new TeleportLocationsCategory(categoryName, new() { location });
                    categories.Add(category);
                    categoryNames.Add(categoryName);
                    categoriesByName.Add(categoryName, category);
                }
            }

            this.allLocations.Clear();
            foreach (TeleportLocationsCategory category in categories)
            {
                this.allLocations.AddRange(category.locations);
                if (categoriesList != null)
                    category.CreateInspectorList();
            }
        }

        public void CreateInspectorLists()
        {
            categoriesList = new("Categories", categoryNames);
            foreach (TeleportLocationsCategory category in categories)
                category.CreateInspectorList();
        }

        public void DestroyInspectorLists()
        {
            categoriesList.OnDisable();
            foreach (TeleportLocationsCategory category in categories)
                category.list.OnDisable();
        }

        public void Draw()
        {
            bool changed = categoriesList.Draw();
            EditorGUILayout.Space();
            foreach (TeleportLocationsCategory category in categories)
            {
                GUILayout.Space(-21f);
                changed |= category.list.Draw();
            }
            if (changed)
                ApplyChanges();
        }

        public void ApplyChanges()
        {
            categories.Clear();
            allLocations.Clear();
            foreach (string categoryName in categoryNames)
            {
                TeleportLocationsCategory category = categoriesByName[categoryName];
                categories.Add(category);
                allLocations.AddRange(category.locations);
            }

            managerSo.Update();
            EditorUtil.SetArrayProperty(
                managerSo.FindProperty("allLocations"),
                allLocations,
                (p, v) => p.objectReferenceValue = v);
            EditorUtil.SetArrayProperty(
                managerSo.FindProperty("categoryNames"),
                categoryNames,
                (p, v) => p.stringValue = v);
            managerSo.ApplyModifiedProperties();
        }
    }

    internal class TeleportLocationsCategory
    {
        public string categoryName;
        public List<TeleportLocation> locations;
        public TeleportLocationsEditorList list;

        public TeleportLocationsCategory(string categoryName, List<TeleportLocation> locations)
        {
            this.categoryName = categoryName;
            this.locations = locations;
        }

        public void CreateInspectorList()
        {
            list = new(categoryName, locations);
        }
    }

    internal class TeleportLocationsCategoriesDummy : DummyForDraggableList<string> { }

    internal class TeleportLocationsCategoriesEditorList : DraggableList<TeleportLocationsCategoriesDummy, string>
    {
        public TeleportLocationsCategoriesEditorList(string header, List<string> entries) : base(header, entries) { }

        protected override void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty prop = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            GUI.Label(rect, prop.stringValue);
        }
    }

    internal class TeleportLocationsDummy : DummyForDraggableList<TeleportLocation> { }

    internal class TeleportLocationsEditorList : DraggableList<TeleportLocationsDummy, TeleportLocation>
    {
        public TeleportLocationsEditorList(string header, List<TeleportLocation> entries) : base(header, entries) { }

        protected override void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty prop = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            TeleportLocation location = (TeleportLocation)prop.objectReferenceValue;
            GUI.Label(rect, location.DisplayName);
            rect.x += rect.width - 60f - 2f - 60f;
            rect.width = 60f;
            if (GUI.Button(rect, "Ping"))
                EditorGUIUtility.PingObject(location);
            rect.x += 60f + 2f;
            if (GUI.Button(rect, "Focus"))
            {
                var prevSelection = Selection.objects;
                Selection.activeObject = location;
                SceneView.FrameLastActiveSceneView();
                Selection.objects = prevSelection;
            }
        }
    }

    internal class DummyForDraggableList<T> : ScriptableObject
    {
        public List<T> entries;
    }

    internal abstract class DraggableList<TDummy, TEntry>
        where TDummy : DummyForDraggableList<TEntry>
    {
        private TDummy dummy;
        private SerializedObject dummySo;
        private SerializedProperty entriesProp;
        protected ReorderableList reorderableList;

        public DraggableList(string header, List<TEntry> entries)
        {
            dummy = ScriptableObject.CreateInstance<TDummy>();
            dummy.entries = entries;
            dummySo = new(dummy);
            entriesProp = dummySo.FindProperty(nameof(dummy.entries));

            reorderableList = new ReorderableList(
                dummySo,
                entriesProp,
                draggable: true,
                displayHeader: true,
                displayAddButton: false,
                displayRemoveButton: false);

            reorderableList.elementHeight = EditorGUIUtility.singleLineHeight;
            reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
            reorderableList.drawElementCallback = DrawListElement;
        }

        public void OnDisable()
        {
            Object.DestroyImmediate(dummy);
        }

        protected abstract void DrawListElement(Rect rect, int index, bool isActive, bool isFocused);

        public bool Draw()
        {
            dummySo.Update();
            reorderableList.DoLayoutList();
            return dummySo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
