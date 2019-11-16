using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace SarRP.Editor
{
    [CustomEditor(typeof(SardineRenderPipelineAsset))]
    public class SarRenderPipelineAssetEditor : UnityEditor.Editor
    {
        UnityEditorInternal.ReorderableList reorderable;
        private void OnEnable()
        {
            reorderable = new UnityEditorInternal.ReorderableList(serializedObject, serializedObject.FindProperty("m_RenderPasses"), true, true, true, true);
            reorderable.drawHeaderCallback =
                (rect) =>
                {
                    EditorGUI.LabelField(rect, "Render Pass");
                };
            reorderable.drawElementCallback =
                (rect, index, isActive, isFocus) =>
                {
                    var obj = reorderable.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += (rect.height - EditorGUIUtility.singleLineHeight) / 4;
                    EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), obj, GUIContent.none);
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