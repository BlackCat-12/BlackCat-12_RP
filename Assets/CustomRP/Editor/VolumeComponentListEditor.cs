using System;
using System.Collections.Generic;
using CustomRP.Runtime.Volume;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CustomRP.Editor
{
    public class VolumeComponentListEditor
    {
        // 追踪profile对象和其序列化接口
        private UnityEditor.Editor m_ParentEditor;
        private SerializedObject m_SerializedObject;
        
        // 获取profile的effect序列化属性
        private SerializedProperty m_ComponentsProperty;
        private SerializedProperty m_PostFX_PrePassProperty; 

        // 存储所有的组件编辑器
        private List<VolumeComponentEditor> m_ComponentEditors = new List<VolumeComponentEditor>();

        // 引用的 VolumeProfile
        public VolumeProfile volumeProfile { get; private set; }

        // 后处理提前处理pass

        // 构造函数，传入保存着profile序列化数据的Volume编辑器
        public VolumeComponentListEditor(UnityEditor.Editor editor)
        {
            m_ParentEditor = editor;
        }

        // 用传入的profile序列化对象，创建用于在hierarchy显示的effectEditor列表
        public void Init(VolumeProfile profile, SerializedObject serializedObject)
        {
            volumeProfile = profile;
            m_SerializedObject = serializedObject;
            m_ComponentsProperty = m_SerializedObject.FindProperty("components"); // FindProperty() 只是提供对某个序列化字段的访问，并不会触发数据同步
            m_PostFX_PrePassProperty = m_SerializedObject.FindProperty("ProfilePostFXPrePasses");

            RefreshEditors();   // 创建effectEditor，用于hierarchy中的编辑
        }

        // 刷新组件编辑器列表，并初始化
        private void RefreshEditors()
        {
            ClearEditors();
            m_SerializedObject.Update();  // 取到最新的序列化数据
            // 从序列化对象中获取effect列表
            Debug.Log("Effect" + m_ComponentsProperty.arraySize);
            for (int i = 0; i < m_ComponentsProperty.arraySize; i++)
            {
                var componentProp = m_ComponentsProperty.GetArrayElementAtIndex(i);
         
                // TODO: 可以填充序列化数据，但其指向对象为null
                var component = componentProp.objectReferenceValue as VolumeComponent;
                
                CreateEffectEditor(component, componentProp);  // 填充effect列表
            }
        }

        // 创建单个effect效果，并加入effect列表
        private void CreateEffectEditor(VolumeComponent component, SerializedProperty property)
        {
            //为目标类型创建对应的编辑器对象，若是找不到对应的编辑器对象则返回Null
          
            var editor = UnityEditor.Editor.CreateEditor(component) as VolumeComponentEditor;
            editor.Init(component, property, m_ParentEditor);
            m_ComponentEditors.Add(editor);
        }

        // 清除effect和预处理列表
        private void ClearEditors()
        {
            if (m_ComponentEditors != null)
            {
                foreach (var editor in m_ComponentEditors)
                {
                    UnityEngine.Object.DestroyImmediate(editor);  // 立即销毁列表对象，不用等待下一帧
                }
                m_ComponentEditors.Clear();  // 清空列表
            }
        }

        // 绘制组件列表的 GUI
        public void OnGUI()
        {
            if (volumeProfile == null)
                return;

            // 更新目标profile数据
            m_SerializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Volume Components", EditorStyles.boldLabel);

            for (int i = 0; i < m_ComponentEditors.Count; i++)
            {
                var editor = m_ComponentEditors[i];
                
                //创建垂直排列包围盒
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                
                // 选择是否开启effect
                editor.component.active = EditorGUILayout.ToggleLeft(editor.GetDisplayTitle(), editor.component.active, EditorStyles.boldLabel);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemoveComponent(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                // 绘制effect参数
                if (editor.component.active)
                {
                    editor.OnInternalInspectorGUI();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Add Component"))
            {
                ShowAddComponentMenu();
            }

            //将修改的序列数据应用回目标数据
            m_SerializedObject.ApplyModifiedProperties();
        }

        // 显示添加组件的菜单
        private void ShowAddComponentMenu()
        {
            var menu = new GenericMenu();

            var types = TypeCache.GetTypesDerivedFrom<VolumeComponent>();
            foreach (var type in types)
            {
                if (type.IsAbstract)
                    continue;

                var displayName = type.Name;
                var attrs = type.GetCustomAttributes(typeof(VolumeComponentMenuAttribute), true);
                if (attrs.Length > 0)
                {
                    var attr = (VolumeComponentMenuAttribute)attrs[0];
                    displayName = attr.menu;
                }

                menu.AddItem(new GUIContent(displayName), false, () => AddComponent(type));
            }

            menu.ShowAsContext();
        }


        // 为Profile添加组件，修改序列化属性
        private void AddComponent(Type type)
        {
            m_SerializedObject.Update();

            var component = ScriptableObject.CreateInstance(type) as VolumeComponent;
            component.name = type.Name;

            // 确保 volumeProfile 已保存为资产
            if (AssetDatabase.GetAssetPath(volumeProfile) == "")
            {
                // 生成一个唯一的资产路径
                string assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Volumes/" + volumeProfile.name + ".asset");

                // 创建资产文件
                AssetDatabase.CreateAsset(volumeProfile, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            // 将后处理Component作为子资产添加到 VolumeProfile
            AssetDatabase.AddObjectToAsset(component, volumeProfile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 更新组件列表
            m_ComponentsProperty.arraySize++;
            var componentProp = m_ComponentsProperty.GetArrayElementAtIndex(m_ComponentsProperty.arraySize - 1);
            componentProp.objectReferenceValue = component;

            AddPreFXPass(component);  // 添加预绘制pass属性

            m_SerializedObject.ApplyModifiedProperties();
            RefreshEditors();
        }


        private void AddPreFXPass(VolumeComponent component)  // 判断是否添加effect预绘制pass
        {
            int ArraySize = m_PostFX_PrePassProperty.arraySize;
            if (component._postFXPrePass != null)
            {
                for (int i = 0; i < ArraySize; i++)
                {
                    if (m_PostFX_PrePassProperty.GetArrayElementAtIndex(i).intValue == (int)component._postFXPrePass.Value)  // 如果发现了有相同的预处理效果，则不添加
                    {
                        return;
                    }
                }
                m_PostFX_PrePassProperty.arraySize++;
                Debug.Log("PrePass" + m_PostFX_PrePassProperty.arraySize);
                var postFXPreProp = m_PostFX_PrePassProperty.GetArrayElementAtIndex(m_PostFX_PrePassProperty.arraySize - 1);
                
                postFXPreProp.intValue = (int)component._postFXPrePass.Value;
            }
        }
        // 移除组件 
        private void RemoveComponent(int index)
        {
            m_SerializedObject.Update();

            var componentProp = m_ComponentsProperty.GetArrayElementAtIndex(index);
            var component = componentProp.objectReferenceValue as VolumeComponent;
            
            // 从列表中移除
            RemovePreFXPass(component);
            m_ComponentsProperty.DeleteArrayElementAtIndex(index);
            
            // 删除资产
            AssetDatabase.RemoveObjectFromAsset(component);
            UnityEngine.Object.DestroyImmediate(component, true);

            m_SerializedObject.ApplyModifiedProperties();
            RefreshEditors();
        }

        private void RemovePreFXPass(VolumeComponent component) // 清除属性中effect的预处理pass
        {
            if (component._postFXPrePass != null)
            {
                int currentPreFXPass = (int)component._postFXPrePass.Value;

                int arraySize = m_ComponentsProperty.arraySize;
                int count = 0;
                for (int i = 0; i < arraySize; i++)
                {
                    var componentProp = m_ComponentsProperty.GetArrayElementAtIndex(i);
                    var neiComponent = componentProp.objectReferenceValue as VolumeComponent;
                    if (neiComponent == null)
                    {
                        Debug.Log("获取Component序列化属性失败");
                    }
                    if (neiComponent._postFXPrePass == null)  // 
                    {
                        continue;
                    }
                    if ((int)component._postFXPrePass.Value == (int)neiComponent._postFXPrePass.Value)
                    {
                        count++;
                    }
                }
                if (count == 1)  // 如果只有当前要removeEffect需要当前preparePass，则删除preparePass
                {
                    for (int i = 0; i < m_PostFX_PrePassProperty.arraySize; i++)
                    {
                        var PrePassProp = m_PostFX_PrePassProperty.GetArrayElementAtIndex(i);
                        var PrePass = PrePassProp.intValue;
                        if (PrePass == currentPreFXPass)
                        {
                            m_PostFX_PrePassProperty.DeleteArrayElementAtIndex(i);
                        }
                    }
                }
            }
        }
        // 清理资源
        public void Clear()
        {
            ClearEditors();
        }
    }
}
