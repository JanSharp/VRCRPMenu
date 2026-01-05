using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace JanSharp
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioSettingManagerWrapper))]
    public class AudioSettingManagerWrapperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                GUILayout.Label("When using the RP Menu, specifically the Voice Range Manager, the defaults "
                    + "defined on the Audio Setting Manager are ignored.", EditorStyles.wordWrappedLabel);
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                GUILayout.Label("This wrapper script must exist in order for the RP Menu system to work, even "
                    + "though it literally does nothing except point to the actual Audio Setting Manager.", EditorStyles.wordWrappedLabel);

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
