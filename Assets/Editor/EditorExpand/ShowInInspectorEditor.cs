using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
// 2. 自定义Editor基类
[CustomEditor(typeof(MonoBehaviour), true)]
public class ShowInInspectorEditor : Editor
{
    private Dictionary<string, object> _parameterValues = new Dictionary<string, object>();
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawButtons();
    }

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    internal void OnSelectionChanged()
    {
        _parameterValues?.Clear();
    }

    private void DrawButtons()
    {
        var methods = target.GetType().GetMethods(
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes(typeof(ShowInInspector), true);
            if (attributes.Length == 0) continue;

            var buttonAttribute = attributes[0] as ShowInInspector;
            var buttonName = string.IsNullOrEmpty(buttonAttribute.ButtonName)
                ? method.Name
                : buttonAttribute.ButtonName;

            DrawMethodButton(method, buttonName, buttonAttribute.ButtonHeight);
        }
    }

    private void DrawMethodButton(MethodInfo method, string name, float height)
    {
        GUILayout.Space(5);
        var parameters = method.GetParameters();
        object[] args = new object[parameters.Length];

        // 绘制参数输入
        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = DrawParameterField(parameters[i]);
        }

        if (GUILayout.Button(name, GUILayout.Height(height)))
        {
            try
            {
                method.Invoke(target, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"执行方法 {name} 失败: {e.InnerException?.Message}");
            }
        }
    }

    private object DrawParameterField(ParameterInfo parameter)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(parameter.Name, GUILayout.Width(120));

        // 生成唯一键（方法名 + 参数名）
        string key = $"{parameter.Member.Name}.{parameter.Name}";
        if (!_parameterValues.TryGetValue(key, out object value))
        {
            value = GetDefaultValue(parameter.ParameterType);
            _parameterValues[key] = value;
        }

        try
        {
            //c# 8.0引入 
            value = parameter.ParameterType switch
            {
                Type t when t == typeof(int) => EditorGUILayout.IntField((int)value),
                Type t when t == typeof(float) => EditorGUILayout.FloatField((float)value),
                Type t when t == typeof(string) => EditorGUILayout.TextField((string)value),
                Type t when t == typeof(bool) => EditorGUILayout.Toggle((bool)value),
                Type t when t == typeof(Vector2) => EditorGUILayout.Vector2Field("", (Vector2)value),
                Type t when t == typeof(Vector3) => EditorGUILayout.Vector3Field("", (Vector3)value),
                Type t when t == typeof(Color) => EditorGUILayout.ColorField((Color)value),
                Type t when t.IsEnum => EditorGUILayout.EnumPopup((Enum)value),
                _ => DrawCustomTypeField(parameter.ParameterType)
            };
        }

       
        catch
        {
            GUILayout.Label($"Unsupported type: {parameter.ParameterType.Name}");
        }
        _parameterValues[key] = value;
       
        GUILayout.EndHorizontal();
        return value;
    }

    private object GetDefaultValue(Type t)
    {
        if (t == typeof(string)) return string.Empty;
        if (t.IsValueType) return Activator.CreateInstance(t);
        return null;
    }

    private object DrawCustomTypeField(Type type)
    {
        GUILayout.Label($"Custom type: {type.Name}");
        return null;
    }
}