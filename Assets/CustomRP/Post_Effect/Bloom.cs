using System;
using UnityEngine;
using CustomRP.Runtime.Volume;
using UnityEngine.Rendering;
using BoolParameter = CustomRP.Runtime.Volume.BoolParameter;
using FloatParameter = CustomRP.Runtime.Volume.FloatParameter;
using VolumeComponent = CustomRP.Runtime.Volume.VolumeComponent;

[Serializable]
[CustomRP.Runtime.Volume.VolumeComponentMenu("Post-processing/Bloom")]
public class Bloom : VolumeComponent,IPostProcessComponent
{
    public FloatParameter intensity = new FloatParameter(1f);
    public FloatParameter threshold1 = new FloatParameter(0f);
    public FloatParameter thresholdKnee = new FloatParameter(0f);
    public FloatParameter maxIteration = new FloatParameter(0f);
    public FloatParameter downScaleLimit = new FloatParameter(1f);
    public BoolParameter bicubicUpsampling = new BoolParameter(false);
    public BoolParameter fadeFireflies = new BoolParameter(false);
    public enum Mode { Additive, Scattering }

    public EnumParameter<Mode> mode = new EnumParameter<Mode>(Mode.Additive);
    public FloatParameter scatter = new FloatParameter(0.5f);
    // 如果需要，您可以在这里添加其他参数

    private int fxSourceID,
        fxSource2ID ;
    
    
    private int bloomBucibicUpsamplingID ,  // Bloom相关
        bloomPrefilterID ,
        bloomThresholdID,
        bloomIntensityID ;
    
    // Bloom效果
    const int maxBloomPyramidLevels = 16;
    int bloomPyramidId;
    
    // public override void GetParameters()
    // {
    //     base.GetParameters();
    //     // 如果有特殊参数处理，可以在这里添加
    // }

    public bool IsActive()
    {
        return enabled.value;
    }
    

    // TODO: effect构造函数修改
    private Bloom()
    {
        //为金字塔的每一层创建贴图ID
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < maxBloomPyramidLevels * 2; i++) {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }
    public void Prepare(bool useHDR)
    {
        
        bloomBucibicUpsamplingID = Shader.PropertyToID("_BloomBicubicUpsampling");  // Bloom相关
        bloomPrefilterID = Shader.PropertyToID("_BloomPrefilter");
        bloomThresholdID = Shader.PropertyToID("_BloomThreshold");
        bloomIntensityID = Shader.PropertyToID("_BloomIntensity");

        _useHDR = useHDR;
    }
    
    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, PostFX_Pass pass, CommandBuffer buffer, Material material)
    {
        buffer.SetGlobalTexture(fxSourceID, from);  // 设置全局渲染源纹理
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        // 进行绘制
        buffer.DrawProcedural(Matrix4x4.identity, material, (int)pass, MeshTopology.Triangles, 3);
    }
    
    public void Render(CommandBuffer cmd, Camera camera, int fxSourceID, Material material)
    {
       cmd.BeginSample("Bloom");
        
        //从setting获取配置属性
        
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
        
        //设置阈值曲线
        Vector4 threshold;
        // TODO: 取值？
        threshold.x = Mathf.GammaToLinearSpace(threshold1.value);
        threshold.y = threshold.x * thresholdKnee.value;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        cmd.SetGlobalVector(bloomThresholdID, threshold);
        
        RenderTextureFormat format = _useHDR? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        cmd.GetTemporaryRT(
            bloomPrefilterID, width, height, 0, FilterMode.Bilinear, format
        );
        Draw(fxSourceID, bloomPrefilterID, fadeFireflies.value 
            ? PostFX_Pass.BloomPrefilterFireflies : PostFX_Pass.BloomPrefilter, cmd, material);
        width /= 2;
        height /= 2;
        
        int fromId = bloomPrefilterID, toId = bloomPyramidId + 1;
        
        int i;
        for (i = 0; i < maxIteration.value; i++) {
            if (height < downScaleLimit.value * 2 || width < downScaleLimit.value * 2|| intensity.value <= 0f ) {
                break;
            }
            
            //在进行降采样的同时，进行一次水平竖直高斯模糊
            int midId = toId - 1;
            cmd.GetTemporaryRT(
                midId, width, height, 0, FilterMode.Bilinear, format
            );
            cmd.GetTemporaryRT(
                toId, width, height, 0, FilterMode.Bilinear, format
            );
            Draw(fromId, midId, PostFX_Pass.BloomHorizontal, cmd, material);
            Draw(midId, toId, PostFX_Pass.BloomVertical, cmd, material);
            
            //Shader按新属性申请的顺序分配标识符
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        
        //设置全局的Float变量，选择是否开启双三次插值上采样
        cmd.SetGlobalFloat(
            bloomBucibicUpsamplingID, bicubicUpsampling.value ? 1f : 0f
        );
        
        //进行上采样，累计变化
        cmd.ReleaseTemporaryRT(fromId - 1);
        toId -= 5;
        cmd.SetGlobalFloat(bloomIntensityID, 1f);
        for (i -= 1; i > 0; i--) {
            cmd.SetGlobalTexture(fxSource2ID, toId + 1);
            Draw(fromId, toId, PostFX_Pass.BloomCombinePass, cmd, material);
            cmd.ReleaseTemporaryRT(fromId);
            cmd.ReleaseTemporaryRT(toId + 1);
            fromId = toId;
            toId -= 2;
        }
        
        cmd.SetGlobalFloat(bloomIntensityID, intensity.value);
        cmd.SetGlobalTexture(fxSource2ID, fxSourceID);
        Draw(fromId, fxSourceID, PostFX_Pass.BloomCombinePass, cmd, material);
        cmd.ReleaseTemporaryRT(fromId);
        cmd.ReleaseTemporaryRT(bloomPrefilterID);
        cmd.EndSample("Bloom");
    }
}