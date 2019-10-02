// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/GridLayer-MarchingSquares"
{
	Properties
	{
		Thickness("Line Thickness", Range(0.0, 1.5)) = 0.1
		LineFeather("Line Feather", Range(0.0, 2.0)) = 0.1
		Tint("Tint", Color) = (1,1,1)
		Fill("Fill", Range(0.0, 0.99)) = 0.3
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
		
		//Blend SrcAlpha OneMinusSrcAlpha 		// Traditional Transparency
		Blend SrcAlpha One  					// Additive Blending

		Pass
		{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile _ BLINK

				#if SHADER_API_MOBILE || SHADER_API_GLES3 || SHADER_API_GLES
				#define SHADER_USE_TEXTURE
				#endif

				#ifdef SHADER_USE_TEXTURE
					sampler2D Values;
				#else
					StructuredBuffer<float> Values;
				#endif

				// array of marching squares combination
				// as point1.x, point1.y, point2.x, point2.y
				static float4 MSquares[16] =
				{
					float4(0	, 0		, 0		,   0),		// case 0 to be ignored
					float4(0	, 0.5	, 0.5	,   0),		// case 1
					float4(0.5	, 0		, 1		, 0.5),		// case 2
					float4(0	, 0.5	, 1		, 0.5),		// case 3
					float4(1	, 0.5	, 0.5	,   1),		// case 4
					float4(0	, 0.5	, 0.5	,   1),		// extra case n.5, only first of 2 lines
					float4(0.5	, 0		, 0.5	,   1),		// case 6
					float4(0	, 0.5	, 0.5	,   1),		// case 7
					float4(0.5	, 1		, 0		, 0.5),		// case 8
					float4(0.5	, 1		, 0.5	,   0),		// case 9
					float4(0	, 0.5	, 0.5	,   0),		// extra case n.10, only first of 2 lines
					float4(0.5	, 1		, 1		, 0.5),		// case 11
					float4(1	, 0.5	, 0		, 0.5),		// case 12
					float4(1	, 0.5	, 0.5	,   0),		// case 13
					float4(0.5	, 0		, 0		, 0.5),		// case 14
					float4(1	, 1		, 1		,   1)		// case 15 to be ignored
				};

				float4 Tint;
				int CountX;
				int CountY;
				float Thickness;
				float LineFeather;
				float Fill;
				int SelectedValue;
				sampler1D Projection;

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
					v2f o;
					o.pos = UnityObjectToClipPos(v.pos);
					o.uv.xy = v.uv;
					o.uv.zw = float2(1.0 / CountX, 1.0 / CountY);
					return o;
				}

				float alphaByMSquare(float2 uv, float4 row)
				{
					float side = (row.z - row.x)*(row.y - uv.y) - (row.x - uv.x)*(row.w - row.y);
					float distSq = pow(row.z - row.x, 2) + pow(row.w - row.y, 2);
					float dist = abs(side) / distSq;

					// Fill on the right side
					float output = step(0, side) * Fill;

					// Add line
					output += smoothstep(Thickness + LineFeather + 0.01, Thickness, dist);
					//output += step(dist, Thickness);

					return saturate(output);
				}

				float alphaByMSquare2(float2 uv, float4 row1, float4 row2)
				{
					float side1 = (row1.z - row1.x)*(row1.y - uv.y) - (row1.x - uv.x)*(row1.w - row1.y);
					float distSq1 = pow(row1.z - row1.x, 2) + pow(row1.w - row1.y, 2);
					float dist1 = abs(side1) / distSq1;

					float side2 = (row2.z - row2.x)*(row2.y - uv.y) - (row2.x - uv.x)*(row2.w - row2.y);
					float distSq2 = pow(row2.z - row2.x, 2) + pow(row2.w - row2.y, 2);
					float dist2 = abs(side2) / distSq2;

					// Fill on the right side
					float output = step(0, side1) * step(0, side2) * Fill;

					// Add line
					output += smoothstep(Thickness + LineFeather + 0.01, Thickness, dist1)+ smoothstep(Thickness + LineFeather + 0.01, Thickness, dist2);
					//output += step(dist1, Thickness) + step(dist2, Thickness);

					return saturate(output);
				}

				float GetOpacity(float4 uv)
				{
					int x = floor(uv.x * CountX - 0.5);
					int y = floor(uv.y * CountY - 0.5);

					int4 value = 0;

					// binary code
					int sum = 0;
					#ifdef SHADER_USE_TEXTURE
						#if SHADER_API_GLES
							float4 coords = float4(x * uv.z, y * uv.w, (x + 1) * uv.z, (y + 1) * uv.w);
							if (x >= 0 && y >= 0)
							{
								value.x = tex2D(Values, coords.xy);
								sum += 8 * (int)saturate(value.x);
							}
							if (x < CountX - 1 && y >= 0)
							{
								value.y = tex2D(Values, coords.zy);
								sum += 4 * (int)saturate(value.y);
							}
							if (x < CountX - 1 && y < CountY - 1)
							{
								value.z = tex2D(Values, coords.zw);
								sum += 2 * (int)saturate(value.z);
							}
							if (x >= 0 && y < CountY - 1)
							{
								value.w = tex2D(Values, coords.xw);
								sum += (int)saturate(value.w);
							}
						#else
							float4 coords = float4(x * uv.z, y * uv.w, (x + 1) * uv.z, (y + 1) * uv.w);
							if (x >= 0 && y >= 0)
							{
								value.x = tex2D(Values, coords.xy);
								sum |= (int)saturate(value.x) << 3;
							}
							if (x < CountX - 1 && y >= 0)
							{
								value.y = tex2D(Values, coords.zy);
								sum |= (int)saturate(value.y) << 2;
							}
							if (x < CountX - 1 && y < CountY - 1)
							{
								value.z = tex2D(Values, coords.zw);
								sum |= (int)saturate(value.z) << 1;
							}
							if (x >= 0 && y < CountY - 1)
							{
								value.w = tex2D(Values, coords.xw);
								sum |= (int)saturate(value.w);
							}
						#endif
					#else
						int index = x + y * CountX;
						if (x >= 0 && y >= 0)
						{
							value.x = Values[index];
							sum |= (int)saturate(value.x) << 3;
						}
						if (x < CountX - 1 && y >= 0)
						{
							value.y = Values[index + 1];
							sum |= (int)saturate(value.y) << 2;
						}
						if (x < CountX - 1 && y < CountY - 1)
						{
							value.z = Values[index + 1 + CountX];
							sum |= (int)saturate(value.z) << 1;
						}
						if (x >= 0 && y < CountY - 1)
						{
							value.w = Values[index + CountX];
							sum |= (int)saturate(value.w);
						}
					#endif

					float outcome;
					if (sum == 0)
					{
						outcome = 0;
					}
					else
					{
						if (sum == 15)
						{
							outcome = Fill;
						}
						else
						{
							// 0.5 to move cell relative position back
							float2 celUV = float2((uv.x * CountX + 0.5) % 1, ((1 - uv.y) * CountY + 0.5) % 1);

							// extra case n.5 with 2 lines  
							if (sum == 5)
							{
								outcome = alphaByMSquare2(celUV, MSquares[5], MSquares[13]);
							}
							// extra case n.10 with 2 lines  
							else if (sum == 10)
							{
								outcome = alphaByMSquare2(celUV, MSquares[14], MSquares[11]);
							}
							else
							{
								outcome = alphaByMSquare(celUV, MSquares[sum]);
							}
						}

						#if BLINK
						value -= SelectedValue;
						float blink = saturate(abs(value.x * value.y * value.z * value.w));
						outcome = saturate(lerp(saturate(outcome * 3.0) * (sin(_Time.y * 5) * 0.6 + 0.5), outcome, blink));
						#endif
					}

					return outcome;
				}

				#include "GridLayer_Utils.cginc"
			
				fixed4 frag(v2f i) : SV_Target
				{
					i.uv.y = ProjectUV(Projection, i.uv.y, CountY);

					float cellOpacity = GetOpacity(i.uv);

					return float4(Tint.rgb, Tint.a * cellOpacity);
				}

			ENDCG
		}
	}
}
