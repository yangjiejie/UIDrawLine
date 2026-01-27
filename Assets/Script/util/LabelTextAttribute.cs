
using UnityEngine;
[System.Diagnostics.Conditional("UNITY_EDITOR")] // 仅unity编辑器下有效，包体内直接裁剪  
public class LabelTextAttribute : PropertyAttribute
{
    public string label;
    public string showCondition;
    public string editorCondition;
    public LabelTextAttribute(string label, string showCondition = null, string editorCondition = null)
    {
        this.label = label;
        this.showCondition = showCondition;
        this.editorCondition = editorCondition;
    }

}