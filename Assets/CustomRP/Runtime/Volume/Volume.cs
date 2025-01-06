using System;

namespace CustomRP.Runtime.Volume
{
    using UnityEngine;

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Custom Volume")]
    public class Volume : MonoBehaviour
    {
        public bool isGlobal = true;
        public float blendDistance = 0f;
        public float weight = 1f;
        public float priority = 0f;
        //public VolumeProfile sharedProfile;
        [HideInInspector]
        public VolumeProfile profile;

        // 获取实际使用的 Profile
        public VolumeProfile profileRef
        {
            get
            {
                return profile;
            }
        }

        // 判断是否有实例化的 Profile
        public bool HasInstantiatedProfile()
        {
            return profile != null;
        }

        // 通过VolumeManager管理生命周期
        private void OnEnable()
        {
            VolumeManager.instance.Register(this);
        }

        private void OnDisable()
        {
            VolumeManager.instance.Unregister(this);
        }
    }

}