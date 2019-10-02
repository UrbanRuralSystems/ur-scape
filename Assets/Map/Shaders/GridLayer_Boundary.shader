// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/GridLayer_Boundary"
{
	Properties
	{
		Tint("Tint", Color) = (1,1,1)
		Alpha ("Alpha", Range(0, 1)) = 1
		Length("Length", Range(0.001, 0.5)) = 0.1
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

				float4 Tint;
	            float Alpha;
				float Length;
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
				};

				v2f vert(appdata v)
				{
					float2 size = mul(unity_ObjectToWorld, float3(1, 1, 1)).xz;
					float thickness = min(Thickness, Length);
					float smoothness = min(Smoothness, Thickness * 0.5);

					v2f o;
					o.pos = UnityObjectToClipPos(v.pos);
					o.uv.xy = v.uv;
					o.uv.zw = size.xy;
					
					o.k.x = thickness;
					o.k.y = smoothness;
					o.k.z = 0.5 / smoothness;
					o.k.w = 0.5 / Length;

					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					float2 K = i.uv.zw * (1 - abs(2 * i.uv - 1));
					float k = min(K.x, K.y);

					float opacity = 1;
					opacity *= saturate(i.k.z * k);
					opacity *= 1 - saturate(i.k.z * (k - 2 * i.k.x + 2 * i.k.y));
					opacity *= saturate((1 + i.k.y - (1 + 0.5 * i.k.y - abs(1 - i.uv.x * 2)) * i.k.w * i.uv.z) * i.k.z);
					opacity *= saturate((1 + i.k.y - (1 + 0.5 * i.k.y - abs(1 - i.uv.y * 2)) * i.k.w * i.uv.w) * i.k.z);

					return float4(Tint.rgb, Tint.a * Alpha * saturate(opacity));
				}

			ENDCG
		}
	}
}
