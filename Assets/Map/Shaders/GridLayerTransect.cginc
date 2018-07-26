// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)
// Summary:

#ifndef GRIDLAYERTRANSECT_INCLUDED
#define GRIDLAYERTRANSECT_INCLUDED

// Common Properties
int CountY;
float CellHalfSize;

sampler1D Projection;

float TransectPosition = 0.5;
float4 TransectTint;


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
	o.uv = v.uv;
	return o;
}

 float GetOpacity(float2 coords)
{
	float cellY = (coords.y * CountY) % 1 - 0.5;
	float min = (CellHalfSize + 0.15)*(CellHalfSize + 0.15) + 0.0001;
	float max = CellHalfSize * CellHalfSize;
	return smoothstep(min, max, cellY*cellY);
}

#include "GridLayer_Utils.cginc"

fixed4 frag(v2f i) : SV_Target
{
#if TRANSECT
	i.uv.y = ProjectUV(Projection, i.uv.y, CountY);

	int y = floor(i.uv.y * CountY);
	int transect = floor(TransectPosition * CountY);

	float cellOpacity = saturate(1 - abs(transect - y)) * GetOpacity(i.uv);

	return float4(TransectTint.rgb, TransectTint.a * cellOpacity);
#else
	return 0;
#endif
}


#endif
