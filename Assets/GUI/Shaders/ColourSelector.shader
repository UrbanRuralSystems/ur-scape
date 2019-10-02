// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/ColourSelector"
{
	Properties
	{
		Value("Value", Range(0.0, 1.0)) = 1
		[KeywordEnum(HueSaturation, HueBrightness, SaturationBrightness)] Mode("Mode", Float) = 0
		//[KeywordEnum(None, Luma1, Luma2)] Luma("Luma", Float) = 1
		//LumaT("Luma Threshold", Range(0.0, 1.0)) = 0.83

		[Header(Ranges)] MinHue("MinHue", Range(0, 1)) = 0
		MaxHue("MaxHue", Range(0, 1)) = 1
		MinSaturation("MinSaturation", Range(0, 1)) = 0
		MaxSaturation("MaxSaturation", Range(0, 1)) = 1
		MinBrightness("MinBrightness", Range(0, 1)) = 0
		MaxBrightness("MaxBrightness", Range(0, 1)) = 1

		// This section is required for shaders used in UI
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		[HideInInspector] _Color("Tint", Color) = (1,1,1,1)
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
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
		}

		// This section is required for shaders used in UI
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
		ZTest[unity_GUIZTestMode]
		
		//Blend SrcAlpha OneMinusSrcAlpha 		// Traditional Transparency

		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile MODE_HUESATURATION MODE_HUEBRIGHTNESS MODE_SATURATIONBRIGHTNESS
			//#pragma multi_compile LUMA_NONE LUMA_LUMA1 LUMA_LUMA2

			float Value;
			float MinHue;
			float MaxHue;
			float MinSaturation;
			float MaxSaturation;
			float MinBrightness;
			float MaxBrightness;
			//float LumaT;

			static const float4 HSV_TO_RGB_K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			inline float3 hsv2rgb(float3 c)
			{
				float3 p = abs(frac(c.xxx + HSV_TO_RGB_K.xyz) * 6.0 - HSV_TO_RGB_K.www);
				return c.z * lerp(HSV_TO_RGB_K.xxx, clamp(p - HSV_TO_RGB_K.xxx, 0.0, 1.0), c.y);
			}

			struct v2f
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert (v2f i)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(i.pos);
				o.uv.xy = i.uv;
				return o;
			}
	
			fixed4 frag(v2f i) : SV_Target
			{
#if MODE_HUESATURATION
				float3 hsv = float3(i.uv, Value);
#elif MODE_HUEBRIGHTNESS
				float3 hsv = float3(i.uv.x, Value, i.uv.y);
#elif MODE_SATURATIONBRIGHTNESS
				float3 hsv = float3(Value, i.uv);
#endif
				hsv.x = lerp(MinHue, MaxHue, hsv.x);
				hsv.y = lerp(MinSaturation, MaxSaturation, hsv.y);
				hsv.z = lerp(MinBrightness, MaxBrightness, hsv.z);

				float3 color = hsv2rgb(hsv);

//#if LUMA_LUMA1
//				color += step(0.2126 * color.r + 0.7152 * color.g + 0.0722 * color.b, LumaT);
//				color *= 0.5;
//#elif LUMA_LUMA2
//				color += step(0.299 * color.r + 0.587 * color.g + 0.114 * color.b, LumaT);
//				color *= 0.5;
//#endif

				return float4(color, 1);
			}

			ENDCG
		}
	}
}
