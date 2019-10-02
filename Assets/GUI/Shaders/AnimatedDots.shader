// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/AnimatedDots"
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

		Dark("Dark", Color) = (0.2,0.2,0.2,1)
		Speed("Speed", Float) = 1
		BeforeFadeIn("Before Fade In", Range(0,1)) = 0
		FadeIn("Fade In", Range(0.001,1)) = 0.2
		Pause("Pause", Range(0,1)) = 0
		FadeOut("Fade Out", Range(0.001,1)) = 1
		AfterFadeOut("After Fade Out", Range(0,1)) = 0
		ScrollSpeed("Scroll Speed", Float) = 0.5
		ScrollEase("Scroll Ease", Range(2,16)) = 5
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

		Fog{ Mode Off }

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		LOD 100

		Pass
		{
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			sampler2D _MainTex;
			float4 Dark;
			float Speed;
			float BeforeFadeIn;
			float FadeIn;
			float Pause;
			float FadeOut;
			float AfterFadeOut;
			float ScrollSpeed;
			float ScrollEase;

			float random(float2 st)
			{
				return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453123);
			}

			struct v2f
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float4 consts : TEXCOORD1;
			};

			v2f vert (v2f i)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(i.pos);
				o.uv = i.uv;
				o.color = i.color;

				float totalTime = BeforeFadeIn + FadeIn + Pause + FadeOut + AfterFadeOut;
				o.consts.x = _Time.y * Speed / totalTime;
				o.consts.y = totalTime / FadeIn;
				o.consts.z = totalTime / FadeOut;
				o.consts.w = 1 / totalTime;
				return o;
			}
	
			fixed4 frag(v2f i) : SV_Target
			{
				float s = _Time.y * ScrollSpeed;
				float scale = 0.5 / tanh(ScrollEase);
				float k = saturate(tanh(ScrollEase * 2 * (s % 1.0) - ScrollEase) * scale + 0.5);
				k += floor(s);
				i.uv.x += k;

				float val = random(floor(i.uv));
				val = (val + i.consts.x) % 1.0;
				val = saturate((val - BeforeFadeIn * i.consts.w) * i.consts.y) * saturate(i.consts.z - (val + AfterFadeOut * i.consts.w) * i.consts.z);
				val *= saturate(_Time.y);

				float4 color = tex2D(_MainTex, i.uv);
				color.rgb *= lerp(Dark, i.color, val);
				//color.rgb = float3(step(0.1 * i.uv.x, (i.consts.x % 1.0))*step(i.uv.y, 0.1),0,0)*(1-color.a) + color.rgb*(color.a);
				//color.a = 1;

				return saturate(color);
			}

			ENDCG
		}
	}
}
