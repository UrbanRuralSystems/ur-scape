// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker (neudecker@arch.ethz.ch)

#ifndef GRIDLAYER_INCLUDED
#define GRIDLAYER_INCLUDED

#if SHADER_API_MOBILE || SHADER_API_GLES3 || SHADER_API_GLES
#define SHADER_USE_TEXTURE
#endif

// Common Properties
#ifdef SHADER_USE_TEXTURE
sampler2D Values;
sampler2D Mask;
#else
StructuredBuffer<float> Values;
StructuredBuffer<uint> Mask;
#endif
int CountX;
int CountY;
float Thickness;
float CellHalfSize;
float OffsetX;
float OffsetY;

sampler1D Projection;


struct appdata
{
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
};

struct v2f
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 worldPos : TEXCOORD1;
	float4 constants : TEXCOORD2;
};

#include "GridLayer_Utils.cginc"
#include "MapLayerUtils.cginc"

v2f vert(appdata v)
{
	float cellHalfSize = unity_ObjectToWorld._m00 / (CountX + 1);
	float fresnel = saturate(1.9 * (0.97 - normalize(_WorldSpaceCameraPos).y)) / cellHalfSize * 0.003;
	float feather = saturate(pow(0.004 / cellHalfSize, 2));

	v2f o;
	v.pos.x += OffsetX / CountX;
	v.pos.y += OffsetY / CountY;
	o.pos = UnityObjectToClipPos(v.pos);
	o.uv = v.uv;
	o.worldPos = mul(unity_ObjectToWorld, v.pos);
	o.constants = float4(saturate(1.0 - cellHalfSize * 50.0), length(_WorldSpaceCameraPos), fresnel, feather);

	return o;
}

inline float GetCellOpacity(float2 coords, float4 constants, float distCamToPixel)
{
#if SHAPE_NONE
	return 1;
#else

	float satDist = saturate((distCamToPixel - constants.y) * constants.z);
	float halfSize = lerp(CellHalfSize, 0.5, saturate(constants.x + satDist));
	float feather = clamp(constants.w + satDist, 0.02, 1);

	float cellX = (coords.x * CountX) % 1 - 0.5;
	float cellY = (coords.y * CountY) % 1 - 0.5;

#if SHAPE_CIRCLE
	float cellMin = pow(halfSize + feather, 2) + 0.0001;
	float cellMax = halfSize * halfSize;
	return smoothstep(cellMin, cellMax, cellX*cellX + cellY*cellY);
#elif SHAPE_SQUARE
	float cellMin = halfSize + feather + 0.0001;
	float cellMax = halfSize;
	return smoothstep(cellMin, cellMax, abs(cellX)) * smoothstep(cellMin, cellMax, abs(cellY));
#endif

#endif
}

inline float GetCellOpacityNoData(float2 uv, float constant, float distCamToPixel)
{
	float extraThickness = Thickness + CellHalfSize * distCamToPixel * 0.02;
	float2 celUV = float2((uv.x * CountX + 0.5) % 1, ((1 - uv.y) * CountY + 0.5) % 1);

	// First line
	float side = celUV.x - celUV.y;
	float dist = abs(side) * 0.5;
	float output = smoothstep(extraThickness, Thickness, dist);

	// Second line
	side = 1 - celUV.x - celUV.y;
	dist = abs(side) *  0.5;
	output += smoothstep(extraThickness, Thickness, dist);

	return saturate(output * (4 / distCamToPixel) * (0.1 - constant));
}

fixed4 frag(v2f i) : SV_Target
{
	i.uv.y = ProjectUV(Projection, i.uv.y, CountY);

	uint index = floor(i.uv.x * CountX) + floor(i.uv.y * CountY) * CountX;

	// Get the value and check if it needs to be filtered
#ifdef SHADER_USE_TEXTURE
	float value = tex2D(Values, i.uv);
#else
	// Calculate the value
	#if CATEGORIZED || !INTERPOLATE
		float value = Values[index];
	#else
		float x = i.uv.x * CountX - 0.5;
		float y = i.uv.y * CountY - 0.5;
		int x1 = clamp(floor(x), 0, CountX - 1);
		int y1 = clamp(floor(y), 0, CountY - 1);
		int x2 = clamp(ceil(x), 0, CountX - 1);
		int y2 = clamp(ceil(y), 0, CountY - 1);
		float alphaX = x % 1;
		float alphaY = y % 1;
		float v1 = lerp(Values[x1 + y1 * CountX], Values[x2 + y1 * CountX], alphaX);
		float v2 = lerp(Values[x1 + y2 * CountX], Values[x2 + y2 * CountX], alphaX);
		float value = lerp(v1, v2, alphaY);
	#endif
#endif

	float4 color = GetColor(value);

	// Get the pixel opacity for the current cell
	float distCamToPixel = distance(i.worldPos, _WorldSpaceCameraPos);
	float cellOpacity = GetCellOpacity(i.uv, i.constants, distCamToPixel);

	// Get the mask for this cell
#if USE_MASK
	uint maskIndex = index / 4;
	uint byteIndex = index - maskIndex * 4;

	#ifdef SHADER_USE_TEXTURE
		uint maskCountX = (CountX + 1) / 2;
		uint maskCountY = (CountY + 1) / 2;
		uint my = maskIndex / maskCountX;
		uint mx = maskIndex - my * maskCountX;
		float4 mask4 = tex2D(Mask, float2((mx + 0.5) / maskCountX, (my + 0.5) / maskCountY));
		float mask = mask4[byteIndex] * 255;
	#else
		float mask = (Mask[maskIndex] >> (byteIndex * 8)) & 1;
	#endif

	#if SHOW_NODATA
		float noDataOpacity = GetCellOpacityNoData(i.uv, i.constants.x, distCamToPixel);
		color.a = lerp(noDataOpacity, color.a, mask);
	#else
		color.a *= mask;
	#endif

#endif

	color.a *= UserOpacity * ToolOpacity * cellOpacity;
	return saturate(color);
}

#endif
