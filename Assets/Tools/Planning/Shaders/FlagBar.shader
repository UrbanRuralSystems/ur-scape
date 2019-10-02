// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/FlagBar"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" { }
		Color1("Color positive 1", Color) = (1,0,0,1)
		Color2("Color positive 2", Color) = (0,1,0,1)
		Percent("Percent", Range(0, 1)) = 0.5
		ArrowHeight("Arrow Height", Range(0, 2)) = 1
		ArrowOrientation("Arrow Orientation", Range(0, 1)) = 1

	}
	SubShader
	{
		LOD 100
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
			"DisableBatching" = "True"
		}

		Fog{ Mode Off }
		Lighting Off
		ZWrite Off
		Cull Off

		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				float4 Color1;
		        float4 Color2;
				float Percent;
				float ArrowHeight;
				float ArrowOrientation;

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

				v2f vert(appdata v)
				{
					float4 zero = UnityObjectToClipPos(float3(0, 0, 0));
					float4 one = UnityObjectToClipPos(float3(100, 100, 100));

					v2f o;
					o.pos = UnityObjectToClipPos(v.pos);
					o.uv.xy = v.uv;
					o.uv.zw = one.xy - zero.xy;

					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					float corners = step(-abs(i.uv.y - ArrowOrientation) - (i.uv.x - 0.5) * ArrowHeight, 0);
					corners *= step(-abs(i.uv.y - ArrowOrientation) + (i.uv.x - 0.5) * ArrowHeight, 0);
					
					//float4 colorTypology = Color1 *ArrowOrientation + Color3 *(1 - ArrowOrientation);
					//float4 colorAttribute = Color2 *ArrowOrientation + Color4 *(1 - ArrowOrientation);
					//
				    fixed4 color = Percent < i.uv.y ? Color1 : Color2;

					return fixed4(color.rgb, color.a * corners);
				}
			ENDCG
		}
	}
}
