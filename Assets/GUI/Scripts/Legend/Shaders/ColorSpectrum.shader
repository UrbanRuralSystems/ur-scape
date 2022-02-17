// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

Shader "URS/ColorSpectrum"
{
	Properties
	{
		[HideInInspector] _MainTex("Sprite Texture", 2D) = "white" {}
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

		Fog{ Mode Off }
		Lighting Off
		ZWrite Off
		Cull Off

		Blend SrcAlpha OneMinusSrcAlpha	// Traditional transparency
		//Blend One OneMinusSrcAlpha		// Premultiplied transparency
		//Blend One One					// Additive

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			fixed4 MinColor;
            fixed4 MaxColor;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				fixed4 color = lerp(MinColor, MaxColor, i.uv.x);
				return color;
			}
			ENDCG
		}
	}
}
