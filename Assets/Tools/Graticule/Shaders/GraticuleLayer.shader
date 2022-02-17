// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/GraticuleLayer"
{
	Properties
	{
		Thickness("Line Thickness", Range(0.00001, 1.0)) = 0.1
		Smoothness("Line Smoothness", Range(0.0001, 1)) = 0.1
		Tint("Tint", Color) = (1,1,1)
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

				#pragma multi_compile _ USE_DEGREES
				#pragma multi_compile _ LOCKED


				float4 Tint;
				float Thickness;
				float Smoothness;

				float2 Interval;
				float2 Offset;

				sampler1D Projection;
				int CountY;

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

				#include "Assets/Map/Shaders/GridLayer_Utils.cginc"
			
				fixed4 frag(v2f i) : SV_Target
				{
					float2 uv = i.uv;
					float2 threshold = Thickness * 0.5;

#if USE_DEGREES
					uv.y = ProjectUV(Projection, uv.y, CountY);
					float diff = 2 - abs(i.uv.y - tex1D(Projection, i.uv.y).x);
					threshold.y *= diff;
#endif
#if LOCKED
					float2 pos = abs(((uv + 500.5 * Interval - 0.5 + threshold) % Interval) - threshold);
#else
					float2 pos = abs(((uv + Offset + 500 * Interval - 0.5 + threshold) % Interval) - threshold);
#endif
					float2 start = threshold * (1 - Smoothness);
					float2 smooth = smoothstep(threshold, start, pos);
					float cellOpacity = saturate(smooth.x + smooth.y);

					return float4(Tint.rgb, Tint.a * cellOpacity);
				}

			ENDCG
		}
	}
}
