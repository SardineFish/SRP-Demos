using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace SarRP.Editor
{
    [CustomEditor(typeof(SarRP.Renderer.PostprocessPass))]
    public class PostprocessPassAssetEditor : UnityEditor.Editor
    {
        UnityEditorInternal.ReorderableList reorderable;
        private void OnEnable()
        {
            reorderable = new UnityEditorInternal.ReorderableList(serializedObject, serializedObject.FindProperty("m_PostProcessSettings"), true, true, true, true);
            reorderable.drawHeaderCallback =
                (rect) =>
                {
                    EditorGUI.LabelField(rect, "Post-process settings");
                };
            reorderable.drawElementCallback =
                (rect, index, isActive, isFocus) =>
                {
                    var obj = reorderable.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += (rect.height - EditorGUIUtility.singleLineHeight) / 4;
                    EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), obj, typeof(Postprocess.PostprocessAsset), GUIContent.none);
                };
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            reorderable.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }

}