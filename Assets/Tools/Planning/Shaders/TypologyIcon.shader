// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

Shader "URS/TypologyIcon"
{
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_FogColor("Fog Color", Color) = (0.3, 0.4, 0.7, 1.0)
		_offset("Mist offset", Range(0, 1)) = 0.1
		_strength("Mist strength",  Range(1, 100.0)) = 15
		_center("Mist center",  Range(1, 30.0)) = 5
		_shadows("Shadows",  Range(0, 1.0)) = 0.1

	}
	SubShader {

		LOD 100

		Tags {
		    "Queue" = "Transparent"
		    "RenderType" = "Transparent" 
			"IgnoreProjector" = "True"
	    }

		Fog{ Mode Off }
		Lighting Off
		// Cull Off // only for testing
		CGPROGRAM
	
        #pragma surface surf Lambert alpha finalcolor:mycolor vertex:myvert

		struct Input {
	
			float2 uv_MainTex;
			float3 worldPos;
			half fog;
		};

		fixed4 _Color;
		float _center;
		float2 myVertex;

		void myvert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			float4 hpos = UnityObjectToClipPos(v.vertex);
			hpos.xy /= hpos.w;
			
			data.fog = clamp(0, 0.8, dot(hpos.xy, hpos.xy) * _center);
		}

		float _offset;
		float _shadows;
		float _strength;
		fixed4 _FogColor;

		void surf(Input IN, inout SurfaceOutput o) {
			o.Albedo = _Color;
			o.Alpha = IN.worldPos.y * _strength - _offset;
		}

		void mycolor(Input IN, SurfaceOutput o, inout fixed4 color) {   
			fixed3 lerpedColor = lerp(_Color.rgb, _FogColor.rgb, IN.fog);
			color.rgb = lerp(lerpedColor, color, _shadows);
		}

		ENDCG
	}
	FallBack "Diffuse"
}
