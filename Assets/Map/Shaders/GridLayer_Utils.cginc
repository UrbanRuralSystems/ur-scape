#ifndef GRIDLAYER_UTILS
#define GRIDLAYER_UTILS

inline float ProjectUV(sampler1D proj, float v, int count)
{
	// Convert Normalized Mercator to Normalized WGS84
	float value = v * count;
	int index = floor(value);
	float invProjCount = 1.0 / (count + 1);
	return lerp(tex1D(proj, (index + 0.5) * invProjCount), tex1D(proj, (index + 1.5) * invProjCount), value - index);
}

#endif