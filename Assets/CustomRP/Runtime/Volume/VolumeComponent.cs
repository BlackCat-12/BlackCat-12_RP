namespace CustomRP.Runtime.Volume
{
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class VolumeComponent : ScriptableObject
    {
        public bool active = true;

        // 序列化存储effect的参数
        [HideInInspector]
        public List<VolumeParameter> parameters = new List<VolumeParameter>();
        
        public PostFX_PrePass? _postFXPrePass = null;  // 可空类型enum，初始为空

        protected bool _useHDR = false;

        // 获取所有参数
        // public virtual void GetParameters()
        // {
        //     parameters.Clear();
        //     
        //     // 利用反射，获取运行时对象的属性，填充当前的effect参数列表
        //     var fields = GetType().GetFields(
        //         System.Reflection.BindingFlags.Public |
        //         System.Reflection.BindingFlags.NonPublic |
        //         System.Reflection.BindingFlags.Instance);
        //
        //     foreach (var field in fields)
        //     {
        //         if (field.FieldType.IsSubclassOf(typeof(VolumeParameter)))
        //         {
        //             var param = field.GetValue(this) as VolumeParameter;
        //             if (param != null)
        //             {
        //                 parameters.Add(param);
        //             }
        //         }
        //     }
        // }
        // 显示名称
        public string displayName;
        
        // TODO: 修改draw调用
    }

}