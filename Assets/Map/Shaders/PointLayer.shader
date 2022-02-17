// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/PointLayer"
{
	Properties
	{
		Tint("Tint", Color) = (1,1,1)
		Feather("Feather", Range(0.0, 0.5)) = 0.22
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
		//Blend SrcAlpha One  			    	// Additive Blending

		Pass
		{
			CGPROGRAM
				#pragma vertex vert 
				#pragma fragment frag

				//#pragma multi_compile SHAPE_NONE SHAPE_CIRCLE SHAPE_SQUARE
				#pragma multi_compile GRADIENT GRADIENT_REVERSE CATEGORIZED STRIPES_UNIFORM STRIPES_UNIFORM_REVERSE STRIPES_1 STRIPES_2 STRIPES_3 STRIPES_4 STRIPES_5
				#pragma multi_compile NO_FILTER FILTER_DATA
				//#pragma multi_compile DONT_INTERPOLATE INTERPOLATE
				//#pragma multi_compile HIDE_NODATA SHOW_NODATA
				//#pragma multi_compile NO_MASK USE_MASK

				#include "MapLayerUtils.cginc"
				
				struct appdata_t
				{
					float4 pos : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 color : TEXCOORD1;
				};

				//float4 Tint;
				float Feather;

				float4x4 TRS;
				float3 Units;		// minUnits.x, minUnits.y, metersToUnits
				int MinMetersX;
				int MinMetersY;

#ifdef SHADER_USE_TEXTURE
				sampler2D Values;
#else
				StructuredBuffer<float> Values;
				StructuredBuffer<int2> Coords;
#endif

				v2f vert(appdata_t i, uint instanceID: SV_InstanceID) {

					float4 pos = i.pos;
					pos = mul(TRS, pos);
					pos.xz += (Coords[instanceID] - int2(MinMetersX, MinMetersY)) * Units.z + Units.xy;

					float4 color = GetColor(Values[instanceID]);
					color.a *= UserOpacity * ToolOpacity;

					v2f o;
					// IMPORTANT: don't use UnityObjectToClipPos. It uses the MVP matrix which contains garbage in the 'M' part.
					// o.pos = UnityObjectToClipPos(pos);
					o.pos = mul(UNITY_MATRIX_VP, pos);
					o.uv = i.uv;
					o.color = color;
					return o;
				}

				float4 frag(v2f i) : SV_Target{
					float2 coords = i.uv - 0.5;
					float cellMin = 0.25;
					float cellMax = cellMin - Feather - 0.0001;
					float k = smoothstep(cellMin, cellMax, dot(coords, coords));

					return fixed4(i.color.rgb, i.color.a * k);
				}

			ENDCG
		}
	}
}
