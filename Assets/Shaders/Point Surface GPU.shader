Shader "Graph/Point Surface GPU" {

	Properties {
		_Smoothness ("Smoothness", Range(0,1)) = 0.5
	}
	
	SubShader {
		CGPROGRAM
		#pragma surface ConfigureSurface Standard fullforwardshadows
		#pragma instancing_options  assumeuniformscaling  procedural:ConfigureProcedural
		#pragma editor_sync_compilation
		#pragma target 4.5
		
		#include "PointGPU.hlsl"
		struct Input {
			float3 worldPos;
		};

		float _Smoothness;

		// #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		// 	StructuredBuffer<float2> particlePositions;
		// #endif

		// float step;

		// void ConfigureProcedural () 
		// {
		// 	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		// 	float2 position = particlePositions[unity_InstanceID];

		// 	unity_ObjectToWorld = 0.0;
		// 	unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 0 , 1.0);
		// 	unity_ObjectToWorld._m00_m11_m22 = step;
		// 	#endif
		// }

		void ConfigureSurface (Input input, inout SurfaceOutputStandard surface) {
			surface.Albedo.rg = saturate(input.worldPos.xy * 0.5 + 0.5);
			surface.Smoothness = _Smoothness;
		}
		ENDCG
	}
						
	FallBack "Diffuse"
}