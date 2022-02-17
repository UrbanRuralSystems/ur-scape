// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

Shader "URS/GridLayer_Weighted"
{
	Properties
	{
		CellHalfSize("Cell Half Size", Range(0.001, 0.9)) = 0.4
		Tint("Tint", Color) = (1,1,1)
		Thickness("Line Thickness", Range(0.0, 0.5)) = 0.01
		[KeywordEnum(None, Circle, Square)] Shape("Shape", Float) = 0
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

		Blend SrcAlpha OneMinusSrcAlpha 		// Traditional Transparency
		//Blend SrcAlpha One  			    	// Additive Blending

		Pass
		{
			CGPROGRAM
				#pragma vertex vert 
				#pragma fragment frag

				#pragma multi_compile SHAPE_NONE SHAPE_CIRCLE SHAPE_SQUARE
				#pragma multi_compile GRADIENT CATEGORIZED STRIPES_UNIFORM STRIPES_1 STRIPES_2 STRIPES_3 STRIPES_4 STRIPES_5
				#pragma multi_compile NO_FILTER FILTER_DATA
				#pragma multi_compile DONT_INTERPOLATE INTERPOLATE
				#pragma multi_compile HIDE_NODATA SHOW_NODATA
				#pragma multi_compile NO_MASK USE_MASK
				#include "GridLayer.cginc"
			ENDCG
		}
	}
}
