// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/AnimatedProgress"
{
	Properties
	{
		// This section is required for shaders used in UI
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		[HideInInspector] _Color("Tint", Color) = (1,1,1,1)
		[HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil("Stencil ID", Float) = 0
		[HideInInspector] _StencilOp("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask("Color Mask", Float) = 15

		Highlight("Highlight", Color) = (0.5,0.5,0.5,1)
		Speed("Speed", Float) = 1
		Size("Size", Range(0.0, 1.0)) = 0.1
		Width("Width", Float) = 1
		TotalWidth("Total Width", Float) = 1

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

			float4 Highlight;
			float Speed;
			float Size;
			float Width;
			float TotalWidth;

			struct v2f
			{
				float4 pos : POSITION;
				float4 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			v2f vert (v2f i)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(i.pos);
				o.uv.xy = i.uv;
				o.uv.z = TotalWidth / max(Width, 0.001);
				o.uv.w = Size * o.uv.z;
				o.color = i.color;
				return o;
			}
	
			fixed4 frag(v2f i) : SV_Target
			{
				float ratio = i.uv.z;
				float size = i.uv.w;

				float t = (_Time.y * Speed * ratio) % ratio;

				// Linear
				//float k = -2;
				//float a = 1 / Size;
				//k += saturate(abs(t - i.uv.x) * a);
				//k += saturate(abs(1 + t - i.uv.x) * a);
				//k += saturate(abs(-1 + t - i.uv.x) * a);

				// Smooth
				float k = 0;
				k += smoothstep(t, t - size, i.uv.x);
				k += smoothstep(t, t + size, i.uv.x);
				t += ratio;
				k *= smoothstep(t, t - size, i.uv.x);
				t -= 2 * ratio;
				k *= smoothstep(t, t + size, i.uv.x);

				return lerp(i.color, Highlight, k);
			}

			ENDCG
		}
	}
}
