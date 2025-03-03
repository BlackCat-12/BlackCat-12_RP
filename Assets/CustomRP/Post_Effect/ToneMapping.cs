using System;
using CustomRP.Runtime.Volume;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using BoolParameter = CustomRP.Runtime.Volume.BoolParameter;
using VolumeComponent = CustomRP.Runtime.Volume.VolumeComponent;

namespace CustomRP.Post_Effect
{
    [Serializable]
    [CustomRP.Runtime.Volume.VolumeComponentMenu("Post-processing/ToneMapping")]
    public class ToneMapping : VolumeComponent,IPostProcessComponent
    {
        public enum Mode { None, ACES, Neutral ,Reinhard}
        
        public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64 }

        public EnumParameter<Mode> mode = new EnumParameter<Mode>(Mode.None);
        
        public EnumParameter<ColorLUTResolution> colorLUTResolution =
            new EnumParameter<ColorLUTResolution>(ColorLUTResolution._32);

        // 使用默认值初始化
        public StructParameter<ColorAdjustmentsSettings > colorAdjustmentss = 
            new StructParameter<ColorAdjustmentsSettings >(new ColorAdjustmentsSettings ()
            {
                colorFilter = Color.white
            });

        public StructParameter<WhiteBalanceSettings> whiteBalance =
            new StructParameter<WhiteBalanceSettings>(new WhiteBalanceSettings());

        public StructParameter<SplitToningSettings> splitToning =
            new StructParameter<SplitToningSettings>(new SplitToningSettings()
            {
                shadows = Color.gray,
                highlights = Color.gray
            });

        public StructParameter<ChannelMixerSettings> channelMixer =
            new StructParameter<ChannelMixerSettings>(new ChannelMixerSettings()
            {
                red = Vector3.right,
                green = Vector3.up,
                blue = Vector3.forward
            });

        public StructParameter<ShadowsMidtonesHighlightsSettings> smh =
            new StructParameter<ShadowsMidtonesHighlightsSettings>(new ShadowsMidtonesHighlightsSettings()
            {
                shadows = Color.white,
                midtones = Color.white,
                highlights = Color.white,
                shadowsEnd = 0.3f,
                highlightsStart = 0.55f,
                highLightsEnd = 1f
            });
        
        
        // TODO:fIXED
        int
            colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT"),
            colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
            colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLogC"),
            colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments"),
            colorFilterId = Shader.PropertyToID("_ColorFilter"),
            whiteBalanceId = Shader.PropertyToID("_WhiteBalance"),
            splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows"),
            splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights"),
            channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed"),
            channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen"),
            channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue"),
            smhShadowsId = Shader.PropertyToID("_SMHShadows"),
            smhMidtonesId = Shader.PropertyToID("_SMHMidtones"),
            smhHighlightsId = Shader.PropertyToID("_SMHHighlights"),
            smhRangeId = Shader.PropertyToID("_SMHRange");
        
        public bool IsActive()
        {
            return enabled.value;
        }

        public void Prepare(bool useHDR)
        {
            _useHDR = useHDR;
        }

        public void Render(CommandBuffer cmd, Camera camera, int fxSourceID, Material material)
        {
            cmd.BeginSample("ColorGrading");
            
            cmd.SetGlobalVector(colorAdjustmentsId, new Vector4(
                Mathf.Pow(2f, colorAdjustmentss.value.postExposure),
                colorAdjustmentss.value.contrast * 0.01f + 1f,
                colorAdjustmentss.value.hueShift * (1f / 360f),
                colorAdjustmentss.value.saturation * 0.01f + 1f
            ));
            cmd.SetGlobalColor(colorFilterId, colorAdjustmentss.value.colorFilter.linear);
            
            //  白平衡
            cmd.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(
                whiteBalance.value.temperature, whiteBalance.value.tint
            ));
            
            //  分离色调
            Color splitColor = splitToning.value.shadows;
            splitColor.a = splitToning.value.balance * 0.01f;
            cmd.SetGlobalColor(splitToningShadowsId, splitColor);
            cmd.SetGlobalColor(splitToningHighlightsId, splitToning.value.highlights);
            
            //  颜色混合器
            cmd.SetGlobalVector(channelMixerRedId, channelMixer.value.red);
            cmd.SetGlobalVector(channelMixerGreenId, channelMixer.value.green);
            cmd.SetGlobalVector(channelMixerBlueId, channelMixer.value.blue);
            
            //  阴影色 中色 高光
            cmd.SetGlobalColor(smhShadowsId, smh.value.shadows.linear);
            cmd.SetGlobalColor(smhMidtonesId, smh.value.midtones.linear);
            cmd.SetGlobalColor(smhHighlightsId, smh.value.highlights.linear);
            cmd.SetGlobalVector(smhRangeId, new Vector4(
                smh.value.shadowsStart, smh.value.shadowsEnd, smh.value.highlightsStart, smh.value.highLightsEnd
            ));
            
            int lutHeight = (int)colorLUTResolution.value;
            int lutWidth = lutHeight * lutHeight;
            cmd.GetTemporaryRT(
                colorGradingLUTId, lutWidth, lutHeight, 0,
                FilterMode.Bilinear, RenderTextureFormat.DefaultHDR
            );
            cmd.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
                lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)
            ));
            
            // 获取中间叫唤纹理
            RenderTextureFormat format = _useHDR? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            cmd.GetTemporaryRT(fxSource2ID, camera.pixelWidth, camera.pixelWidth, 0, FilterMode.Bilinear, format);
            
            PostFX_Pass pass = PostFX_Pass.ColorGradingNone + (int)mode.value;
            
            cmd.SetGlobalFloat(
                colorGradingLUTInLogId, _useHDR && pass != PostFX_Pass.ColorGradingNone ? 1f : 0f
            );
            Draw(fxSourceID, colorGradingLUTId, pass, cmd, material);

            cmd.SetGlobalVector(colorGradingLUTParametersId,
                new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f)
            );
            
            // 交换纹理并写回
            Draw(fxSourceID, fxSource2ID, PostFX_Pass.CopyWithLinear, cmd, material);
            Draw(fxSource2ID, fxSourceID, PostFX_Pass.Final, cmd, material);
            
            cmd.ReleaseTemporaryRT(fxSource2ID);
            cmd.ReleaseTemporaryRT(colorGradingLUTId);
            cmd.EndSample("ColorGrading");
        }
    }
}
