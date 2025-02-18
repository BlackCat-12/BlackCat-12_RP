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

    private CommandBuffer _buffer = new CommandBuffer()
    {
        name = bufferName
    };

    private ScriptableRenderContext context;
    private PostFXSettings settings;

    private Camera _camera;
    public bool IsNeedDepthNormalTex => true;

    public bool isActive = false;

    private Shader _postFXShader = Shader.Find("Hidden/Custom RP/Post FX Stack");
    private Material _material;

    public static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    private static int depthNormalId = Shader.PropertyToID("_DepthNormalTex"), // 全局纹理属性
        surfaceIdDepthId = Shader.PropertyToID("_SurfaceIdDepthTex");
    
    private static int fxSourceID = Shader.PropertyToID("_PostFXSource"),
        fxSource2ID = Shader.PropertyToID("_PostFXSource2");


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
        ScriptableRenderContext context, Camera camera, PostFXSettings settings
    ) {
        this.context = context;
        this._camera = camera;
        this.isActive = camera.cameraType <= CameraType.SceneView;
        
        // 为nameId创建存储摄像机输出的RT
        _buffer.GetTemporaryRT(frameBufferId, _camera.pixelWidth, _camera.pixelHeight, 
            32, FilterMode.Point, RenderTextureFormat.Default);
        this.context.ExecuteCommandBuffer(_buffer);
        
        //配置关闭Scene界面的后处理显示
        ApplySceneViewState();
    }

    public void PreparePostFX()
    {
        
    }

    public void Render(int sourceId)
    {
        List<CustomRP.Runtime.Volume.Volume> volumeComponents = VolumeManager.instance.GetVolumes(_camera);
        VolumeProfile volumeProfile = VolumeManager.instance.GetVolumeProfile(_camera, volumeComponents);
        
        _buffer.GetTemporaryRT(fxSourceID, _camera.pixelWidth, _camera.pixelHeight,  // 为输入id绑定一张运行时本地的RT，方便后处理堆栈中的绘制
             32, FilterMode.Point, RenderTextureFormat.Default);           // 此时，fxSourceID的Shader属性为摄像机后处理前输出，而其标识的本地RT为空
        _buffer.SetGlobalTexture(fxSourceID, sourceId);  // 将摄像机RT，设置为后处理堆栈Shader的全局属性
        
        foreach (var component in volumeProfile.components)  // 后处理准备阶段
        {
            if (component.active && component is IPostProcessComponent postProcessComponent)
            {
                postProcessComponent.Prepare();
            }
        }
        foreach (var component in volumeProfile.components)  // 后处理阶段
        {
            if (component.active && component is IPostProcessComponent postProcessComponent)
            {
                postProcessComponent.Render(_buffer, _camera, fxSourceID, Material);   // 上一次后处理效果的输出作为下一次的输入
            }
        }
        
        Draw(fxSourceID,  BuiltinRenderTextureType.CameraTarget, PostFX_Pass.CopyWithLinear);
        
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    public void Cleanup()
    {
        _buffer.ReleaseTemporaryRT(fxSourceID);
         _buffer.ReleaseTemporaryRT(frameBufferId); // 释放 _CameraFrameBuffer
    }
    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, PostFX_Pass pass)
    {
        _buffer.SetGlobalTexture(fxSourceID, from);  // 设置全局渲染源纹理
        _buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        
        // 进行绘制
        _buffer.DrawProcedural(Matrix4x4.identity, _material, (int)pass, MeshTopology.Triangles, 3);
    }
    

}
