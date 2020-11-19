// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/Transect Category"
{
	Properties
	{
		Thickness("Thickness", Range(0.5,4)) = 2.75

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

			#if defined(SHADER_USE_TEXTURE)
				sampler2D Values;
			#else
				StructuredBuffer<float> Values;
			#endif

			int Count;			// number of values
			float Thickness;
			float Width;
			float Height;
			
			fixed4 CategoryColors[256];
			float InvCountMinusOne;

			struct appdata
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f p) : SV_Target
			{
				int index = floor(p.uv.x * Count);
				#ifdef SHADER_USE_TEXTURE
					float value = tex2D(Values, float2((float)index / Count, 0));
				#else
					float value = Values[index];
				#endif
			
				float yPos = value * InvCountMinusOne;
				//float grid = saturate(sin(p.uv.y * 100 - 1));
				float grid = p.uv.y / (yPos);

				int category = round(value);
				fixed4 color = CategoryColors[category];
				color.a = step(p.uv.y ,yPos) * step(yPos - 0.1, p.uv.y) * grid ;

				return color;
			}

			ENDCG
		}
	}
}
