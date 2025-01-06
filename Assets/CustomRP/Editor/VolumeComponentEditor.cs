using UnityEngine;

using CustomRP.Runtime.Volume;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(VolumeComponent), true)]
public class VolumeComponentEditor : Editor
{
    public VolumeComponent component { get; private set; }
    private SerializedProperty m_SerializedProperty;
    private Editor m_ParentEditor;

    //追踪序列化参数，用于在inspector中绘制和修改
    private List<SerializedDataParameter> m_Parameters;

    // 初始化component，构建运行时实例化参数列表和序列化参数列表
    public void Init(VolumeComponent component, SerializedProperty property, Editor parentEditor)
    {
        this.component = component;
        m_SerializedProperty = property;
        m_ParentEditor = parentEditor;

        // 运行时component获取实例化参数列表
        //component.GetParameters();
        m_Parameters = new List<SerializedDataParameter>();

        //创建当前component属性的序列化参数列表
        var fields = component.GetType().GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        // 利用反射获取参数属性，并利用其Name获取其对应的序列化属性
        foreach (var field in fields)
        {
            if (field.FieldType.IsSubclassOf(typeof(VolumeParameter)))
            {
                var prop = serializedObject.FindProperty(field.Name);
                if (prop != null)
                {
                    m_Parameters.Add(new SerializedDataParameter(prop));
                }
            }
        }
    }

    // 获取显示的标题
    public string GetDisplayTitle()
    {
        var title = component.displayName;
        if (string.IsNullOrEmpty(title))
            title = ObjectNames.NicifyVariableName(component.GetType().Name);
        return title;
    }

    // 绘制组件的属性
    public void OnInternalInspectorGUI()
    {
        serializedObject.Update();

        foreach (var param in m_Parameters)
        {
            PropertyField(param);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // 绘制单个属性,修改序列化参数
    protected void PropertyField(SerializedDataParameter property)
    {
        EditorGUILayout.BeginHorizontal();

        // 绘制 overrideState
     
        //EditorGUILayout.PropertyField(overrideProp, GUIContent.none, GUILayout.MaxWidth(20));
        
        // 绘制参数值，提供秀改接口
        EditorGUILayout.PropertyField(property.value, new GUIContent(property.displayName));

        EditorGUILayout.EndHorizontal();
    }
}

// 辅助类，用于封装序列化的参数
public class SerializedDataParameter
{
    public SerializedProperty value;
    public SerializedProperty overrideState;
    public string displayName;

    public SerializedDataParameter(SerializedProperty property)
    {
        value = property.FindPropertyRelative("m_Value");
        overrideState = property.FindPropertyRelative("overrideState");
        displayName = ObjectNames.NicifyVariableName(property.name);
    }
}
