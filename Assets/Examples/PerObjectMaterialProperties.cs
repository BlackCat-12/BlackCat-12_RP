using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

//同一Mono只允许添加最多一个本组件
[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    //获取Shader的缓存序列
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int cutoffId = Shader.PropertyToID("_CutOff"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoothness");
    //静态变量，保存着用于使用的Material属性
    static MaterialPropertyBlock block;
    
    [SerializeField]
    Color baseColor = Color.cyan;

    [SerializeField, Range(0.0f, 1.0f)] private float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;
    
    private void OnValidate()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutoffId, cutoff);
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Awake()
    {
        OnValidate();
    }
}