Shader "Hidden/Custom RP/Post FX Stack/DeferredLightPass"
{
    SubShader
    {
        Cull Off ZWrite On ZTest On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #define PI 3.14159265358

            // D 返回微表面法线分布影响的光照强度
            float Trowbridge_Reitz_GGX(float NdotH, float a)
            {
                float a2     = a * a;
                float NdotH2 = NdotH * NdotH;

                float nom   = a2;
                float denom = (NdotH2 * (a2 - 1.0) + 1.0);
                denom = PI * denom * denom;

                return nom / denom;
            }

            // F
            float3 SchlickFresnel(float HdotV, float3 F0)
            {
                float m = clamp(1-HdotV, 0, 1);
                float m2 = m * m;
                float m5 = m2 * m2 * m; // pow(m,5)
                return F0 + (1.0 - F0) * m5;
            }

            // G 返回自遮蔽影响的光照强度
            float SchlickGGX(float NdotV, float k)
            {
                float nom   = NdotV;
                float denom = NdotV * (1.0 - k) + k;

                return nom / denom;
            }

            // 直接光照
            float3 PBR(float3 N, float3 V, float3 L, float3 albedo, float3 radiance, float roughness, float metallic)
            {
                roughness = max(roughness, 0.05);   // 保证光滑物体也有高光

                float3 H = normalize(L+V);
                float NdotL = max(dot(N, L), 0);
                float NdotV = max(dot(N, V), 0);
                float NdotH = max(dot(N, H), 0);  // NdotH是半角向量和法线的点积
                float HdotV = max(dot(H, V), 0);
                float alpha = roughness * roughness;
                float k = ((alpha+1) * (alpha+1)) / 8.0;
                float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);  // 垂直观察物体时的反射

                float  D = Trowbridge_Reitz_GGX(NdotH, alpha);
                float3 F = SchlickFresnel(HdotV, F0);
                float  G = SchlickGGX(NdotV, k) * SchlickGGX(NdotL, k);

                float3 k_s = F;
                float3 k_d = (1.0 - k_s) * (1.0 - metallic);
                float3 f_diffuse = albedo / PI;
                float3 f_specular = (D * F * G) / (4.0 * NdotV * NdotL + 0.0001);

                f_diffuse *= PI;
                f_specular *= PI;
                float3 color = (k_d * f_diffuse + f_specular) * radiance * NdotL;  // Cook-Torrance BRDF

                return color;
            }

            // Unity Use this as IBL F
            float3 FresnelSchlickRoughness(float NdotV, float3 f0, float roughness)  // 添加粗糙度，防止边缘过黑
            {
                float r1 = 1.0f - roughness;
                return f0 + (max(float3(r1, r1, r1), f0) - f0) * pow(1 - NdotV, 5.0f);
            }

            // 间接光照
            float3 IBL(
                float3 N, float3 V,
                float3 albedo, float roughness, float metallic,
                samplerCUBE _diffuseIBL, samplerCUBE _specularIBL, sampler2D _brdfLut)
            {
                roughness = min(roughness, 0.99);

                float3 H = normalize(N);    // 用法向作为半角向量
                float NdotV = max(dot(N, V), 0);
                float HdotV = max(dot(H, V), 0);
                float3 R = normalize(reflect(-V, N));   // 反射向量，视角方向的反射

                float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);
                // float3 F = SchlickFresnel(HdotV, F0);
                float3 F = FresnelSchlickRoughness(HdotV, F0, roughness);
                float3 k_s = F;
                float3 k_d = (1.0 - k_s) * (1.0 - metallic);

                // 漫反射
                float3 IBLd = texCUBE(_diffuseIBL, N).rgb;  // 根据法线 N 进行采样
                float3 diffuse = k_d * albedo * IBLd;

                // 镜面反射
                float rgh = roughness * (1.7 - 0.7 * roughness);  
                float lod = 6.0 * rgh;  // Unity 默认 6 级 mipmap，将粗糙度映射为 minmap
                float3 IBLs = texCUBElod(_specularIBL, float4(R, lod)).rgb;  // 高光光照项，反射观察方向
                float2 brdf = tex2D(_brdfLut, float2(NdotV, roughness)).rg;  // brdf项
                float3 specular = IBLs * (F0 * brdf.x + brdf.y);

                float3 ambient = diffuse + specular;

                return ambient;
            }

// ***********************************************************************************            
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

            sampler2D _gdepth;
            sampler2D _GT0;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;

            samplerCUBE _specularIBL;
            samplerCUBE _diffuseIBL;
            sampler2D _brdflut;

            float4x4 _vpMatrix;
            float4x4 _vpMatrixInv;

            fixed4 frag (v2f i, out float depth : SV_Depth) : SV_Target
            {
                float2 uv = i.uv;
                float4 GT2 = tex2D(_GT2, uv);
                float4 GT3 = tex2D(_GT3, uv);

                // 从 Gbuffer 解码数据
                float3 albedo = tex2D(_GT0, uv).rgb;
                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                float2 motionVec = GT2.rg;
                float roughness = GT2.b;
                float metallic = GT2.a;
                float3 emission = GT3.rgb;
                float occlusion = GT3.a;

                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float d_lin = Linear01Depth(d);  // 转换到 0--1 之间的线性深度

                // 反投影重建世界坐标
                float4 ndcPos = float4(uv*2-1, d, 1);  // 重建NDC坐标
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;  // 齐次转换

                // 计算参数
                float3 N = normalize(normal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                float3 radiance = _LightColor0.rgb;

                // 计算光照
                float3 dirCol = PBR(N, V, L, albedo, radiance, roughness, metallic);  // 计算直接光
                float3 envCol = IBL(N, V, albedo, roughness, metallic, _diffuseIBL, _specularIBL, _brdflut);
                float3 finCol = dirCol + envCol * occlusion + emission;
                depth = d;

                return float4(finCol, 1);
            }
            ENDCG
        }
    }
}