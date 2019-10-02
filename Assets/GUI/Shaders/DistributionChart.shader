// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/DistributionChart"
{
	Properties
	{
		Line("Line", Color) = (1,0,0)
		Filtered("Filtered", Color) = (0.5, 0.5, 0.5)

		// This section is required for shaders used within a UI mask
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		[HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil("Stencil ID", Float) = 0
		[HideInInspector] _StencilOp("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask("Color Mask", Float) = 15
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
		}

		// This section is required for shaders used within a UI mask
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
		ColorMask [_ColorMask]

		Fog{ Mode Off }
		Lighting Off
		ZWrite Off
		Cull Off

		LOD 100

		Blend SrcAlpha OneMinusSrcAlpha 		// Traditional Transparency
		//Blend SrcAlpha One  					// Additive Blending

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#if SHADER_API_MOBILE || SHADER_API_GLES3 || SHADER_API_GLES
			#define SHADER_USE_TEXTURE
			#endif

			#ifdef SHADER_USE_TEXTURE
				sampler2D Values;
			#else
				StructuredBuffer<float> Values;
			#endif

			int Count;			// number of lines
			float InvMaxValue;
			float InvHeight;
			float4 Line;
			float4 Filtered;
			float4 Tint;
			float MinRange;
			float MaxRange;
			float MinFilter;
			float MaxFilter;
			float Power;

			struct appdata
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 uv : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.uv.xy = v.uv;
				o.uv.z = 1.0 / Count;
				return o;
			}
	
			fixed4 frag(v2f p) : SV_Target
			{
				p.uv.x = pow(p.uv.x, Power);

				float x = p.uv.x * Count;
		  	 	int index = floor(x);
				#ifdef SHADER_USE_TEXTURE
					float val = tex2D(Values, float2((float)index * p.uv.z, 0));
				#else
					float val = Values[min(index, Count - 1)];
				#endif

				float y = pow(val, 0.4) * InvMaxValue;
				float Min = max(MinFilter, MinRange);
				float Max = min(MaxFilter, MaxRange);
				float4 color = lerp(Filtered, Tint * Line, step(Min, p.uv.x) * step(p.uv.x, Max));
				color.a *= step(p.uv.y, max(y, InvHeight));

				return color;
			}

			ENDCG
		}
	}
}
