// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/Transect"
{
	Properties
	{
		Line("Line", Color) = (1,0,0)
		Fill("Fill", Color) = (1,0,0, 0.5)
		Thickness("Thickness", Range(0.5,4)) = 1.0

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

			#if SHADER_API_MOBILE || SHADER_API_GLES3 || SHADER_API_GLES
			#define SHADER_USE_TEXTURE
			#endif
			
			#ifdef SHADER_USE_TEXTURE
				sampler2D Values;
			#else
				StructuredBuffer<float> Values;
			#endif

			int Count;			// number of lines (= number of values - 1)
			float Thickness;
			float Width;
			float Height;
			float4 Line;
			float4 Fill;

			struct appdata
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 uv : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.uv.xy = v.uv;
				o.uv.z = 1.0 / Count;
				return o;
			}
	
			float onLine(float2 p, float2 pt1, float2 pt2)
			{
				float distSq = (pt1.x - pt2.x)*(pt1.x - pt2.x) + (pt1.y - pt2.y)*(pt1.y - pt2.y);
				float2 projection = lerp(pt1, pt2, saturate(dot(p - pt1, pt2 - pt1) / distSq));
				return step(distance(p, projection), Thickness);
			}

			fixed4 frag(v2f p) : SV_Target
			{
				float x = p.uv.x * Count;
		  	 	int index = floor(x);
				#ifdef SHADER_USE_TEXTURE
					float val1 = saturate(tex2D(Values, float2((float)index * p.uv.z, 0)));
					float val2 = saturate(tex2D(Values, float2((float)(index + 1) * p.uv.z, 0)));
				#else
					float val1 = saturate(Values[index]);
					float val2 = saturate(Values[index + 1]);
				#endif
				float y = lerp(val1, val2, x - index);

				float xNext = p.uv.z;
			    float xPos = index * xNext;

				float2 pt1 = float2(xPos * Width, val1 * Height);
				float2 pt2 = float2((xPos + xNext) * Width, val2 * Height);
				float2 pt = float2(p.uv.x * Width, p.uv.y * Height);
				float dist = onLine(pt, pt1, pt2);

				if (x - index < 0.5)
				{
					#ifdef SHADER_USE_TEXTURE
						float val0 = saturate(tex2D(Values, float2((float)(index - 1) * p.uv.z, 0)));
					#else
						float val0 = saturate(Values[index - 1]);
					#endif
					float2 pt0 = float2((xPos - xNext) * Width, val0 * Height);
					dist = saturate(dist + onLine(pt, pt0, pt1));
				}
				else
				{
					#ifdef SHADER_USE_TEXTURE
						float val3 = saturate(tex2D(Values, float2((float)(index + 2) * p.uv.z, 0)));
					#else
						float val3 = saturate(Values[index + 2]);
					#endif
					float2 pt3 = float2((xPos + 2 * xNext) * Width, val3 * Height);
					dist = saturate(dist + onLine(pt, pt2, pt3));
				}

				float lineAlpha = Line.a * dist;
				float fillAlpha = Fill.a * step(p.uv.y, y) * (1 - lineAlpha);

				return float4(Line.rgb * lineAlpha + Fill.rgb * fillAlpha, lineAlpha + fillAlpha);
			}

			ENDCG
		}
	}
}
