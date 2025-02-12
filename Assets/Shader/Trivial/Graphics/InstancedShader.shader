Shader "Instanced/InstancedShader" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader {

        Pass {

            Tags {"LightMode"="ForwardBase"}

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight// 用于前向渲染的多编译变体，包含基础光照但不包含光照贴图等功能
            #pragma target 4.5 // 指定shader目标级别，4.5支持计算着色器和结构化缓冲区

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"// Unity光照相关函数
            #include "AutoLight.cginc"          // Unity阴影相关函数

            sampler2D _MainTex;

            #if SHADER_TARGET >= 45
                StructuredBuffer<float4> positionBuffer;
            #endif

            struct v2f
            {
                float4 pos : SV_POSITION;         // 裁剪空间位置
                float2 uv_MainTex : TEXCOORD0;    // 主纹理UV
                float3 ambient : TEXCOORD1;       // 环境光
                float3 diffuse : TEXCOORD2;       // 漫反射光
                float3 color : TEXCOORD3;         // 颜色
                SHADOW_COORDS(4)                  // 阴影坐标
            };
            void rotate2D(inout float2 v, float r)
            {
                float s, c;                    
                sincos(r, s, c);                 // 计算sin和cos值
                v = float2(v.x * c - v.y * s, v.x * s + v.y * c);// 2D旋转矩阵变换
            }

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            // instanceID是GPU实例化时的实例索引，自动递增
            {
                // 获取实例数据
                float4 data = positionBuffer[instanceID];
    
                // 计算旋转
                float rotation = data.w * data.w * _Time.x * 0.5f;
                rotate2D(data.xz, rotation);
    
                // 计算世界空间位置
                float3 localPosition = v.vertex.xyz * data.w;  // 应用缩放
                float3 worldPosition = data.xyz + localPosition;// 应用位移
                float3 worldNormal = v.normal;                 // 世界空间法线
    
                // 计算光照
                float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz)); // 法线点乘光照方向
                float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));  // 环境光照
                float3 diffuse = (ndotl * _LightColor0.rgb);           // 漫反射光照
                float3 color = v.color;                                // 顶点色
    
                // 填充输出结构
                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f)); // 转换到裁剪空间
                o.uv_MainTex = v.texcoord;        // 传递UV
                o.ambient = ambient;              // 传递环境光
                o.diffuse = diffuse;             // 传递漫反射光
                o.color = color;                 // 传递颜色
                TRANSFER_SHADOW(o)               // 计算阴影坐标
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed shadow = SHADOW_ATTENUATION(i);  // 获取阴影衰减
                fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);  // 采样主纹理
                float3 lighting = i.diffuse * shadow + i.ambient;  // 计算最终光照
                // 合成最终颜色：纹理 * 顶点色 * 光照
                fixed4 output = fixed4(albedo.rgb * i.color * lighting, albedo.w);
                UNITY_APPLY_FOG(i.fogCoord, output);  // 应用雾效
                return output;
            }

            ENDCG
        }
    }
}
