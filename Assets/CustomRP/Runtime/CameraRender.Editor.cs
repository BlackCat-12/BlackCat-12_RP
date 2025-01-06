using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

partial class CameraRenderer
{
    partial void DrawUnsupportedShaders ();
    partial void DrawGizmosBeforePostFX();
    partial void DrawGizmosAfterPostFX();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();
    
    #if UNITY_EDITOR
    
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };
    
    string SampleName { get; set; }

    private static Material errorMaterial;
    
    partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(_camera))
        {
            overrideMaterial = errorMaterial
        };
        //设置对应的渲染pass
        for(int i = 1; i < legacyShaderTagIds.Length; i++) {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        
        var filteringSettings = FilteringSettings.defaultValue;
        _context.DrawRenderers(
            _cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    //Gizmos绘制
    partial void DrawGizmosBeforePostFX()
    {
        if (UnityEditor.Handles.ShouldRenderGizmos())
        {
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            //_context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawGizmosAfterPostFX()
    {
        if (UnityEditor.Handles.ShouldRenderGizmos())
        {
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void PrepareForSceneWindow()
    {
        //当当前相机渲染场景时
        if (_camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
        }
    }
    partial void PrepareBuffer()
    {
        _buffer.name = SampleName = _camera.name;
    }

#else
    const string SampleName = bufferName;
#endif
}


