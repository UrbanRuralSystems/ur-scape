// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/PinLine"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Power("Power", Float) = 1
		_SquareX("Square X", Float) = 2
		_SquareY("Square Y", Float) = 2
	}
	SubShader
	{
		LOD 100
		Tags
		{
			"Queue" = "Overlay"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
		}

		Fog{ Mode Off }
		Lighting Off
		ZWrite Off
		Cull Off
		ZTest Always

		Blend SrcAlpha OneMinusSrcAlpha 		// Traditional Transparency

		Pass
		{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				float4 _Color;
				float _Power;
				float _SquareX;
				float _SquareY;

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
					o.uv.xy = v.uv;
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					float2 uvy = abs(i.uv * 2.0 - 1.0);
					float2 uvx = i.uv ;
					float alpha = saturate(1 - pow(pow(uvx.x, _SquareX) + pow(uvy.y, _SquareY), _Power));

					return fixed4(_Color.rgb, _Color.a * alpha);
				}
			ENDCG
		}
	}
}
