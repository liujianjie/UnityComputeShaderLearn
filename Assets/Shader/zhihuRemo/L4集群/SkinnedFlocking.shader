Shader "Flocking/Skinned" { 
	Properties{
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Bumpmap", 2D) = "bump" {}
		_MetallicGlossMap("Metallic", 2D) = "white"{}
		_Metallic("Metallic", Range(0, 1)) = 0.0
		_Glossiness("Smoothness", Range(0, 1)) = 1.0
	}
	SubShader{

		CGPROGRAM
        #include "UnityCG.cginc"

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _MetallicGlossMap;
		struct appdata_custom {
			float4 vertex :POSITION;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;
			float4 tangent : TANGENT;

			uint id : SV_VertexID;
			uint inst : SV_InstanceID;

			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct Input{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float3 worldPos;
		};
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		#pragma multi_compile __ FRAME_INTERPOLATION					// 对应cs的FRAME_INTERPOLATION开启关键字
		#pragma surface surf Standard vertex:vert addshadow nolightmap// 自动生成阴影投射代码 不使用光照贴图
		#pragma instancing_options procedural:setup // 启用GPU实例化，并指定setup函数作为实例化数据准备函数

		float4x4 _Matrix;
		int _CurrentFrame;
		int _NextFrame;
		float _FrameInterpolation;
		int numOfFrames;

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			struct Boid
			{
				float3 position;
				float3 direction;// float3 写错成 float 
				float noise_offset;
				float speed;
				float frame;
				float3 padding;
			};
			StructuredBuffer<Boid> boidsBuffer;
			StructuredBuffer<float4> vertexAnimation;
		#endif

		float4x4 create_matrix(float3 pos, float3 dir, float3 up){		// float3 写错成 float 
			float3 zaxis = normalize(dir);
			float3 xaxis = normalize(cross(up, zaxis));
			float3 yaxis = cross(zaxis, xaxis);
			return float4x4(
				xaxis.x, yaxis.x, zaxis.x, pos.x,	
				xaxis.y, yaxis.y, zaxis.y, pos.y,
				xaxis.z, yaxis.z, zaxis.z, pos.z,
				0, 0, 0, 1
			);
		}
		void vert(inout appdata_custom v)
		{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				#ifdef FRAME_INTERPOLATION
					v.vertex = lerp(vertexAnimation[v.id * numOfFrames + _CurrentFrame], vertexAnimation[v.id * numOfFrames + _NextFrame], _FrameInterpolation);// 在上一帧和下一帧的顶点位置进行插值
				#else
					v.vertex = vertexAnimation[v.id * numOfFrames + _CurrentFrame];	// ；打错
				#endif
				v.vertex = mul(_Matrix, v.vertex);
			#endif
		}
		// 在每个实例渲染前被GPU自动调用
		void setup()
		{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				_Matrix = create_matrix(boidsBuffer[unity_InstanceID].position, boidsBuffer[unity_InstanceID].direction, float3(0.0, 1.0, 0.0));
				_CurrentFrame = boidsBuffer[unity_InstanceID].frame;
				#ifdef FRAME_INTERPOLATION
					_NextFrame = _CurrentFrame + 1;
					if (_NextFrame >= numOfFrames) _NextFrame = 0;
					_FrameInterpolation = frac(boidsBuffer[unity_InstanceID].frame);// 取帧的小数作为插值权重
				#endif
			#endif
		}
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			fixed4 m = tex2D(_MetallicGlossMap, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			o.Metallic = m.r;
			o.Smoothness = _Glossiness * m.a;
		}
		ENDCG
	}
}