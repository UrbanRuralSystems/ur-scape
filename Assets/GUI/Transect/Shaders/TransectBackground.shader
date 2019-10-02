// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/TransectBackground"
{
	Properties
	{
		[HideInInspector] Width("Width", Float) = 512
		[HideInInspector] Height("Height", Float) = 512

		BackgroundColor("Backgroundcolor", Color) = (0.0, 0.0, 0.0, 1)
		LargeDivisionColor("LargeDivisionColor", Color) = (0.4, 0.4, 0.4, 1)
		SmallDivisionColor("SmallDivisionColor", Color) = (0.25, 0.25, 0.25, 1)
		LargeDivisions("LargeDivisions", Range(1, 25)) = 14
		SmallDivisions("SmallDivisions", Range(1, 25)) = 10
		LargeDivisionSize("LargeDivisionSize", Range(1, 5.0)) = 1.5
		SmallDivisionSize("SmallDivisionSize", Range(1, 5.0)) = 1
		BorderColor("BorderColor", Color) = (0.5, 0.5, 0.5, 1)
		BorderSize("BorderSize", Range(1,5)) = 1

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

			float Width;
			float Height;
			float4 BackgroundColor;
			float3 LargeDivisionColor;
			float3 SmallDivisionColor;
			int LargeDivisions;
			int SmallDivisions;
			float LargeDivisionSize;
			float SmallDivisionSize;
			float3 BorderColor;
			float BorderSize;

			struct appdata
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.uv.xy = v.uv;
				o.uv.z = 1.0 / SmallDivisions;
				o.uv.w = 0.5 * Width / LargeDivisions;
				return o;
			}

			fixed4 frag (v2f p) : SV_Target
			{
				float xPixel = Width * p.uv.x;

				float k = p.uv.w;  // 0.5 * Width / LargeDivisions
				float X = fmod(xPixel + k, 2.0 * k) - k;
				float large = saturate(LargeDivisionSize - abs(X));

				k *= p.uv.z;	// 1 / SmallDivisions
				X = fmod(xPixel + k, 2.0 * k) - k;
				float small = saturate(SmallDivisionSize - abs(X));

				float3 color = lerp(lerp(BackgroundColor.rgb, SmallDivisionColor.rgb, small), LargeDivisionColor.rgb, large);
				float alpha = BackgroundColor.a;

				float border = step(p.uv.y * (1 - p.uv.y), BorderSize / Height);
				border += step(p.uv.x * (1 - p.uv.x), BorderSize / Width);
				color = lerp(color, BorderColor, border);

				return fixed4(color, alpha);
			}

			ENDCG
		}
	}
}
