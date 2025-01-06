namespace CustomRP.Runtime.Volume
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Rendering/Custom Volume Profile")]
    public class VolumeProfile : ScriptableObject
    {
        // 可序列化的后处理效果列表
        public List<VolumeComponent> components = new List<VolumeComponent>();
        [HideInInspector,SerializeField] private List<PostFX_PrePass> ProfilePostFXPrePasses = new List<PostFX_PrePass>();

        public T Add<T>() where T : VolumeComponent
        {
            var comp = CreateInstance<T>();
            comp.name = typeof(T).Name;
            components.Add(comp);
            return comp;
        }

        public void Remove<T>() where T : VolumeComponent
        {
            components.RemoveAll(c => c is T);
        }

        public bool Has<T>() where T : VolumeComponent
        {
            return components.Exists(c => c is T);
        }

        public T Get<T>() where T : VolumeComponent
        {
            return components.Find(c => c is T) as T;
        }

        // 检查是否只是有一个effect启用
        public bool IsActive()
        {
            bool active = false;
            foreach (var component in components)
            {
                if (component.active)
                {
                    active = true;
                }
            }
            return active;
        }

        public List<PostFX_PrePass> GetPrePostFXPass()
        {
            return ProfilePostFXPrePasses;
        }
    }
}