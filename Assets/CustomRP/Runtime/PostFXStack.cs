using System.Collections;
using System.Collections.Generic;
using CustomRP.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;
using VolumeComponent = CustomRP.Runtime.Volume.VolumeComponent;
using VolumeProfile = CustomRP.Runtime.Volume.VolumeProfile;

public partial class PostFXStack 
{
    private const string bufferName = "Post FX";

    private CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    private ScriptableRenderContext context;
    private PostFXSettings settings;

    private Camera _camera;
    private bool _useHDR;
    public bool IsNeedDepthNormalTex => true;
    public bool isActive = false;

    private Shader _postFXShader = Shader.Find("Hidden/Custom RP/Post FX Stack");
    private Material _material; 
    private static int depthNormalId = Shader.PropertyToID("_DepthNormalTex"), // 全局纹理属性
        surfaceIdDepthId = Shader.PropertyToID("_SurfaceIdDepthTex");
    
    // private static int fxSourceID = Shader.PropertyToID("_PostFXSource"),
    //     fxSource2ID = Shader.PropertyToID("_PostFXSource2");

    private PostFXSettings _postFXSettings;


    public PostFXStack()
    {
        if (_postFXShader == null)
        {
            Debug.Log("Can't find Shader");
            return;
        }
        _material = new Material(_postFXShader);
    }
    public Material Material
    {
        get
        {
            return _material;
        }
    }
    public void Setup (
        ScriptableRenderContext context, Camera camera, PostFXSettings settings, bool useHDR
    ) {
        this.context = context;
        this._camera = camera;
        this.isActive = camera.cameraType <= CameraType.SceneView;
        _postFXSettings = this.settings;

        _useHDR = useHDR;
        //配置关闭Scene界面的后处理显示
        ApplySceneViewState();
    }
    
    public void Render(int sourceId)
    {
        List<CustomRP.Runtime.Volume.Volume> volumeComponents = VolumeManager.instance.GetVolumes(_camera);
        VolumeProfile volumeProfile = VolumeManager.instance.GetVolumeProfile(_camera, volumeComponents);
        
        buffer.GetTemporaryRT(VolumeComponent.fxSourceID, _camera.pixelWidth, _camera.pixelHeight,  // 为输入id绑定一张运行时本地的RT，方便后处理堆栈中的绘制
             32, FilterMode.Point, RenderTextureFormat.Default);           // 此时，fxSourceID的Shader属性为摄像机后处理前输出，而其标识的本地RT为空
        Draw(sourceId, VolumeComponent.fxSourceID, PostFX_Pass.CopyWithLinear);
        buffer.SetGlobalTexture(VolumeComponent.fxSourceID, sourceId);  // 将摄像机RT，设置为后处理堆栈Shader的全局属性
        
        foreach (var component in volumeProfile.components)  // 后处理准备阶段
        {
            if (component.active && component is IPostProcessComponent postProcessComponent)
            {
                postProcessComponent.Prepare(_useHDR);
            }
        }
        foreach (var component in volumeProfile.components)  // 后处理阶段
        {
            if (component.active && component is IPostProcessComponent postProcessComponent)
            {
                postProcessComponent.Render(buffer, _camera, VolumeComponent.fxSourceID, Material);   // 上一次后处理效果的输出作为下一次的输入
            }
        }
        
        Draw(VolumeComponent.fxSourceID,  BuiltinRenderTextureType.CameraTarget, PostFX_Pass.CopyWithLinear);
        
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public void Cleanup()
    {
      //  buffer.ReleaseTemporaryRT(fxSourceID);
        
    }
    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, PostFX_Pass pass)
    {
        buffer.SetGlobalTexture(VolumeComponent.fxSourceID, from);  // 设置全局渲染源纹理
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        
        // 进行绘制
        buffer.DrawProcedural(Matrix4x4.identity, _material, (int)pass, MeshTopology.Triangles, 3);
    }
    

}
