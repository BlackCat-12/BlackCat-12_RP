using UnityEditor;
using UnityEngine;
using CustomRP.Editor;
using CustomRP.Runtime.Volume;


[CustomEditor(typeof(Volume))]
public class VolumeEditor : Editor
{
    SerializedProperty isGlobalProp;
    SerializedProperty blendDistanceProp;
    SerializedProperty weightProp;
    SerializedProperty priorityProp;
    SerializedProperty ProfileProp;
    VolumeComponentListEditor componentList;

    // 每次点击时启用
    void OnEnable()
    {
        //追踪序列化属性
        isGlobalProp = serializedObject.FindProperty("isGlobal");
        blendDistanceProp = serializedObject.FindProperty("blendDistance");
        weightProp = serializedObject.FindProperty("weight");
        priorityProp = serializedObject.FindProperty("priority");
        ProfileProp = serializedObject.FindProperty("profile");
        
        // 将当前编辑的实例化对象转换为volume对象，用来处理非序列化数据
        var volume = target as Volume;
        
        // 创建当前profile的后处理效果列表
        // TODO: 只会对非shred生效
        if (volume.HasInstantiatedProfile())
        {
            // todo：修改每次点击重新初始化
            componentList = new VolumeComponentListEditor(this); 
            // todo：profile和序列化后传入的区别
            componentList.Init(volume.profile, new SerializedObject(volume.profile));
        }
    }

    void OnDisable()
    {
        if (componentList != null)
            componentList.Clear();
    }

    public override void OnInspectorGUI()
    {
        //更新序列化数据，确保正确显示
        serializedObject.Update();

        //在Inspector中显示并编辑属性
        EditorGUILayout.PropertyField(isGlobalProp);
        if (!isGlobalProp.boolValue)
            EditorGUILayout.PropertyField(blendDistanceProp);
        EditorGUILayout.PropertyField(weightProp);
        EditorGUILayout.PropertyField(priorityProp);
        
        //空行
        EditorGUILayout.Space();
        //EditorGUILayout.PropertyField(sharedProfileProp);
        EditorGUILayout.PropertyField(ProfileProp);


        var volume = target as Volume;
        if (volume.profile != null)
        {
            if (volume.HasInstantiatedProfile())
            {
                //跳转到实例的profile
                if (GUILayout.Button("Edit Profile"))
                {
                    Selection.activeObject = volume.profile;
                }

                if (componentList != null)
                {
                    componentList.OnGUI();
                }
            }
            //如果只有共享Profile，没有创建
            // else
            // {
            //     if (GUILayout.Button("Create Profile"))
            //     {
            //         // 实例化 sharedProfile
            //         volume.profile = Instantiate(volume.sharedProfile);
            //         volume.profile.name = volume.sharedProfile.name + " (Instance)";
            //
            //         // 为当前Volume的Profile生成一个唯一的资产路径，将其序列化保存到外存
            //         string assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/CustomRP/Volumes/" + volume.profile.name + ".asset");
            //
            //         // 创建新的资产文件
            //         AssetDatabase.CreateAsset(volume.profile, assetPath);
            //         AssetDatabase.SaveAssets();
            //         AssetDatabase.Refresh();
            //
            //         // 初始化组件列表编辑器,传入本身编辑器
            //         componentList = new VolumeComponentListEditor(this);
            //         componentList.Init(volume.profile, new SerializedObject(volume.profile));
            //     }
            //
            // }
        }
        //对序列化数据应用修改
        serializedObject.ApplyModifiedProperties();
    }
}

