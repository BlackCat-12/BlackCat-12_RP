using System.Collections.Generic;
using UnityEngine;
using CustomRP.Runtime.Volume;

public class VolumeManager
{
    public static readonly VolumeManager instance = new VolumeManager();

    private List<Volume> volumes = new List<Volume>();

    public void Register(Volume volume)
    {
        if (!volumes.Contains(volume))
            volumes.Add(volume);
    }

    public void Unregister(Volume volume)
    {
        volumes.Remove(volume);
    }

    public List<Volume> GetVolumes(Camera camera)
    {
        // 返回所有启用的 Volume
        return volumes.FindAll(v => v.enabled && v.profileRef != null);
    }

    // TODO: 混合profile
    public VolumeProfile GetVolumeProfile(Camera camera, List<Volume> volumes)
    {
        // 根据相机位置和 Volume 的优先级计算当前生效的 VolumeProfile
        VolumeProfile profile = null;
        float highestPriority = float.MinValue;

        foreach (var volume in volumes)
        {
            if (volume.isGlobal || IsCameraWithinVolume(camera, volume))
            {
                if (volume.priority > highestPriority)
                {
                    highestPriority = volume.priority;
                    profile = volume.profileRef;
                }
            }
        }
        return profile;
    }

    // 查看是否至少有一个profile在启用
    public bool CheckEffectActive()
    {
        List<Volume> activeVolume = GetVolumes(Camera.current);
        bool isActive = false;
        foreach (var volume in activeVolume)
        {
            if (volume.profileRef.IsActive())
            {
                isActive = true;
            }
        }
        return isActive;
    }

    public List<PostFX_PrePass> GetPostFXPrePasses()  // 目前只适用于一个profile
    {
        List<PostFX_PrePass> fxPrePasses = new List<PostFX_PrePass>();
        foreach (var volume in volumes)
        {
            if (volume.profileRef.IsActive())
            {
                fxPrePasses = volume.profileRef.GetPrePostFXPass();
            }
        }
        return fxPrePasses;
    }
    private bool IsCameraWithinVolume(Camera camera, Volume volume)
    {
        // 判断相机是否在 Volume 范围内
        // 如果 Volume 不是全局的，可以根据其边界框进行判断
        // 这里需要实现具体的判断逻辑
        return true; // 示例中返回 false，需要根据实际情况实现
    }
}