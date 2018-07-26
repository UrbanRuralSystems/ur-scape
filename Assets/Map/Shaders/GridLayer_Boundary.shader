// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

Shader "URS/GridLayer_Boundary"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1)
		Alpha ("Alpha", Float) =1
		Lenght("Lenght", Range(0.001, 0.5)) = 0.1
		Thickness("Line Thickness", Range(0.001, 0.4)) = 0.1
		Smoothness("Smoothness", Range(0.0001, 0.2)) = 0.0001
	}
	SubShader
	{
		LOD 100

		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
			"DisableBatching" = "True"
		}

		Fog{ Mode Off }
		Lighting Off
		ZWrite Off
		Cull Off

		Blend SrcAlpha OneMinusSrcAlpha 		// Traditional Transparency
		//Blend SrcAlpha One  					// Additive Blending

		Pass
		{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityShaderVariables.cginc"

				float4 _Color;
	            float Alpha;
				float Lenght;
				float Thickness;
				float Smoothness;
	
				struct appdata
				{
					float4 pos : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float4 pos : SV_POSITION;
					float4 uv : TEXCOORD0;
					float4 k : TEXCOORD1;
					float2 s : TEXCOORD2;
				};

				v2f vert(appdata v)
				{
					float thickness = min(Thickness, Lenght);
					float4 size = mul(unity_ObjectToWorld, float4(1, 1, 1, 0));
					float smoothness = min(Smoothness, Thickness * 0.5);
					float2 invSize = 1.0 / size.xz;

					v2f o;
					o.pos = UnityObjectToClipPos(v.pos);
					o.uv.xy = v.uv;
					o.uv.z = 0;
					
					o.k.x = 0.5 * size.x / Lenght;
					o.k.y = 0.5 * size.z / Lenght;
					o.k.z = thickness * invSize.x;
					o.k.w = thickness * invSize.y;
					o.s.x = smoothness * invSize.x;
					o.s.y = smoothness * invSize.y;
					o.uv.w = smoothstep(0.005, 0.01, size.x * size.z);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					float opacity = 1;
					float k = 1 - abs(1.0 - i.uv.x - i.uv.y) - abs(i.uv.x - i.uv.y);
					opacity *= saturate(0.5 / i.s.x * k);
					opacity *= 1 - saturate(0.5 / i.s.y * (k - i.k.z - i.k.w + 2 * i.s.y));
					opacity *= saturate((1 + i.s.x - (1 + 0.5 * i.s.x - abs(1 - i.uv.x * 2)) * i.k.x) * 0.5 / i.s.x);
					opacity *= saturate((1 + i.s.y - (1 + 0.5 * i.s.y - abs(1 - i.uv.y * 2)) * i.k.y) * 0.5 / i.s.y);

					return float4(_Color.rgb, _Color.a * Alpha * saturate(opacity) * i.uv.w);
				}

			ENDCG
		}
	}
}
