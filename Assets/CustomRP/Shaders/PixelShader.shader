Shader "Custom/InnerOuterEdgeDetectionStencil"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _EdgeColorInner ("Inner Edge Color", Color) = (1,0,0,1)
        _EdgeColorOuter ("Outer Edge Color", Color) = (0,1,0,1)
        _EdgeThreshold ("Edge Threshold", Float) = 0.1
        _NormalThreshold ("Normal Threshold", Float) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Name "EdgeDetectionPass"
            ZTest Always Cull Off ZWrite Off
            Stencil
            {
                Ref 1
                Comp always
                Pass replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthNormalsTexture;
            float4 _MainTex_TexelSize;
            fixed4 _EdgeColorInner;
            fixed4 _EdgeColorOuter;
            float _EdgeThreshold;
            float _NormalThreshold;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;

                // 获取当前像素的深度和法线
                float4 currentDepthNormals = tex2D(_CameraDepthNormalsTexture, i.uv);
                float currentDepth = currentDepthNormals.r;
                float3 currentNormal = UnpackNormal(currentDepthNormals);

                // 初始化边缘检测
                bool isEdge = false;
                bool isInnerEdge = false;
                bool isOuterEdge = false;

                // 定义邻域采样方向（上下左右）
                float2 offsets[4] = {
                    float2(-1, 0),
                    float2(1, 0),
                    float2(0, -1),
                    float2(0, 1)
                };

                for(int j = 0; j < 4; j++)
                {
                    float2 offset = offsets[j] * texelSize;
                    float4 neighborDepthNormals = tex2D(_CameraDepthNormalsTexture, i.uv + offset);
                    float neighborDepth = neighborDepthNormals.r;
                    float3 neighborNormal = UnpackNormal(neighborDepthNormals);

                    // 深度差异
                    float depthDiff = currentDepth - neighborDepth;

                    // 法线差异
                    float normalDiff = dot(currentNormal, neighborNormal);

                    // 判断是否为边缘
                    if(abs(depthDiff) > _EdgeThreshold)
                    {
                        isEdge = true;

                        // 根据深度差异判断是内轮廓还是外轮廓
                        if(depthDiff > 0)
                        {
                            // 当前像素在邻居前方，可能是外轮廓
                            if(normalDiff > _NormalThreshold)
                            {
                                isOuterEdge = true;
                            }
                        }
                        else
                        {
                            // 当前像素在邻居后方，可能是内轮廓
                            if(normalDiff > _NormalThreshold)
                            {
                                isInnerEdge = true;
                            }
                        }
                    }
                }

                if(isEdge)
                {
                    if(isInnerEdge && !isOuterEdge)
                        return _EdgeColorInner;
                    else if(isOuterEdge && !isInnerEdge)
                        return _EdgeColorOuter;
                    else
                        return float4(1,1,1,1); // 混合边缘颜色
                }

                // 原始颜色
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
        // 另一Pass，用于绘制物体内部并写入Stencil Buffer
        Pass
        {
            Name "MainPass"
            Tags { "RenderType"="Opaque" }
            ZTest LEqual Cull Off ZWrite On
            Stencil
            {
                Ref 1
                Comp equal
                Pass keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 返回原始颜色
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
