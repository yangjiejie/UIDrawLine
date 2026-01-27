using UnityEditor;
using UnityEngine;
[CustomPropertyDrawer(typeof(LabelTextAttribute), false)]
public class LabelDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = attribute as LabelTextAttribute;
        label.text = attr.label;
        // 如果字段为false则不显示
        if (!string.IsNullOrEmpty(attr.showCondition))
        {
            var field = property.serializedObject.FindProperty(attr.showCondition);
            if (!field.boolValue)
                return;
        }

        // 是否能编辑
        var notEdit = false;
        if (!string.IsNullOrEmpty(attr.editorCondition))
        {
            if (attr.editorCondition == "false")
                notEdit = true;
        }
        if (notEdit)
            GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);

        if (notEdit)
            GUI.enabled = true;
    }

    //如果不显示返回没间隔
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        LabelTextAttribute labelAttr = (LabelTextAttribute)attribute;
        var showCondition = property.serializedObject.FindProperty(labelAttr.showCondition);
        if (showCondition != null)
        {
            bool show = showCondition.boolValue;
            if (!show)
                return -EditorGUIUtility.standardVerticalSpacing;
        }
        return EditorGUI.GetPropertyHeight(property, label);
    }

}