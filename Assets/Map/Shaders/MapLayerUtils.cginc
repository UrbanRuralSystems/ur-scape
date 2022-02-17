// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#ifndef MAPLAYERUTILS_INCLUDED
#define MAPLAYERUTILS_INCLUDED

float4 Tint;
float UserOpacity;
float ToolOpacity;

float MinValue;
float InvValueRange;
float Gamma;

#if CATEGORIZED
	// Categorized Grid Only Properties
	fixed4 CategoryColors[256];
#else
	// Default Grid Only Properties
	float FilterMinValue;
	float FilterMaxValue;
	float4 StripeMarkers;
	float StripesCount;
#endif


float4 GetColor(float value)
{
	float4 color = Tint;

#if CATEGORIZED

	color = CategoryColors[floor(value)];

#else

#if FILTER_DATA
	float filter = step(FilterMinValue, value) * step(value, FilterMaxValue);
#endif


	#if STRIPES_1 | STRIPES_2 | STRIPES_3 | STRIPES_4 | STRIPES_5
		float stripes = StripesCount;

		#if STRIPES_2 | STRIPES_3 | STRIPES_4 | STRIPES_5
			stripes -= step(StripeMarkers[0], value);
		#if STRIPES_3 | STRIPES_4 | STRIPES_5
			stripes -= step(StripeMarkers[1], value);
		#if STRIPES_4 | STRIPES_5
			stripes -= step(StripeMarkers[2], value);
		#if STRIPES_5
			stripes -= step(StripeMarkers[3], value);
		#endif
		#endif
		#endif
		#endif

		value = stripes / StripesCount;
	#else
		// Normalize the value
		value = (value - MinValue) * InvValueRange;

		#if STRIPES_UNIFORM
			value = ceil(value * StripesCount) / StripesCount;
		#elif STRIPES_UNIFORM_REVERSE
			value = ceil(StripesCount - value * StripesCount) / StripesCount;
		#elif GRADIENT_REVERSE
			value = 1 - value;
		#endif

		// Apply gamma (need to saturate value to avoid negative numbers)
		value = pow(saturate(value), Gamma);
	#endif
	
	#if FILTER_DATA
		value *= filter;
	#endif

	color.a = saturate(value);

#endif

	return color;
}

#endif
