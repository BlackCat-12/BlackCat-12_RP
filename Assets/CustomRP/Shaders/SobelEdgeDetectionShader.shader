Shader "Custom/SobelEdgeDetectionShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Threshold", Range(0, 4)) = 1.0
        _ErodeThreshold ("Erode Threshold", Range(0, 9)) = 6
        _DilateThreshold ("Dilate Threshold", Range(0, 9)) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Threshold;
            float _ErodeThreshold;
            float _DilateThreshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Sobel kernels
                float Gx[9] = {
                    -1, 0, 1,
                    -2, 0, 2,
                    -1, 0, 1
                };

                float Gy[9] = {
                    -1, -2, -1,
                     0,  0,  0,
                     1,  2,  1
                };

                // 采样3*3的邻居
                float3 sample[9];
                int idx = 0;
                for(int y = -1; y <= 1; y++)
                {
                    for(int x = -1; x <= 1; x++)
                    {
                        sample[idx] = tex2D(_MainTex, i.uv + float2(x, y) * _MainTex_TexelSize.xy).rgb;
                        idx++;
                    }
                }

                // 转化为灰度阶
                float gray[9];
                for(int j = 0; j < 9; j++)
                {
                    gray[j] = dot(sample[j], float3(0.299, 0.587, 0.114));
                }

                // 应用Sobel算子
                float gx = 0.0;
                float gy = 0.0;
                for(int j = 0; j < 9; j++)
                {
                    gx += Gx[j] * gray[j];
                    gy += Gy[j] * gray[j];
                }
                // 计算梯度值
                float g = abs(gx) + abs(gy);

                // 计算平均值
                float localAvg = 0.0;
                for(int j = 0; j < 9; j++)
                {
                    localAvg += gray[j];
                }
                localAvg /= 9.0;
                
                // 应用侵蚀阈值
                int edgeCount = 0;
                for(int j = 0; j < 9; j++)
                {
                    edgeCount += (gray[j] >= (_Threshold * localAvg)) ? 1 : 0;
                }
                // Erosion
                float eroded = (edgeCount >= _ErodeThreshold) ? 1.0 : 0.0;

                // 应用膨胀阈值
                float dilated = (edgeCount >= _DilateThreshold) ? 1.0 : 0.0;

                // Combine results (example: edge after erosion and dilation)
                float finalEdge = saturate(eroded + dilated);

                return float4(finalEdge, finalEdge, finalEdge, 1.0);
            }
            ENDCG
        }
    }
}
