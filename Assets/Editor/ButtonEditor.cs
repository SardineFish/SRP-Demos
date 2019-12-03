using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEditor;

[CustomAttributeEditor(typeof(EditorButtonAttribute))]
public class ButtonEditor : AttributeEditor
{
    public override void OnEdit(MemberInfo member, CustomEditorAttribute attr)
    {
        var method = member as MethodInfo;
        var buttonAttr = attr as EditorButtonAttribute;
        if (method is null)
            return;

        var lable = buttonAttr.Label == "" ? member.Name : buttonAttr.Label;
        if (GUILayout.Button(lable))
            method.Invoke(target, null);
    }
}
