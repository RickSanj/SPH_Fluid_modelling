Shader "Graph/Point Surface GPU" {

	Properties {
		_Smoothness ("Smoothness", Range(0,1)) = 0.5
		_ParticleRadius ("ParticleRadius", Range(0,10)) = 1
	}
	
	SubShader {
		CGPROGRAM
		#pragma surface ConfigureSurface Standard fullforwardshadows addshadow
		#pragma instancing_options  assumeuniformscaling  procedural:ConfigureProcedural
		#pragma editor_sync_compilation
		#pragma target 4.5
		
		#include "PointGPU.hlsl"
		struct Input {
			float3 worldPos;
		};

		float _Smoothness;

		void ConfigureSurface (Input input, inout SurfaceOutputStandard surface) {
			surface.Albedo.rgb = saturate(float3(input.worldPos.x*0.3, input.worldPos.y*0.6, input.worldPos.x + input.worldPos.y * 0.2));
			surface.Smoothness = _Smoothness;
		}
		ENDCG
	}
						
	FallBack "Diffuse"
}