// 1. 创建按钮特性
using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
[System.Diagnostics.Conditional("UNITY_EDITOR")] // 仅unity编辑器下有效，包体内直接裁剪  
public class ShowInInspector : PropertyAttribute
{
    public string ButtonName { get; }
    public float ButtonHeight { get; }

    public ShowInInspector(string name = "", float height = 20f)
    {
        ButtonName = name;
        ButtonHeight = height;
    }
}