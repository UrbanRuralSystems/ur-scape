// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/PulsatingCell"
{
	Properties
	{
		Tint("Tint", Color) = (1,1,1,1)
		Frequency("Frequency", Range(0.05, 10)) = 5.0
	}
	SubShader
	{
		LOD 100

		Tags
		{
			"Queue" = "Transparent-100"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
		}

		Fog{ Mode Off }
		Lighting Off

		Blend SrcAlpha OneMinusSrcAlpha 		// Traditional Transparency

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 Tint;
			float Frequency;

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

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = cos(pow(abs((i.uv - 0.5) * 2.2), 12)) * 1.5;
				float k1 = (uv.x + uv.y);
				uv = cos(pow(abs((i.uv - 0.5) * 2.2), 12) + 1) * 2.0;
				float k2 = (uv.x + uv.y);
				float k = (k1 - max(1.25 * k2, 0)) * (sin(_Time.y * Frequency) * 0.05 + 0.2);
				return fixed4(Tint.rgb, Tint.a * saturate(k));
			}
			ENDCG
		}
	}
}
