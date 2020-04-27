
#ifndef RECEIVESHADOWS_INCLUDED
#define RECEIVESHADOWS_INCLUDED 

inline float FragHartShadows(float4 shadowCoord,sampler2D _actShadowMap,fixed _bias,fixed _strength,fixed _texmapScale)
{
	shadowCoord.w = step(shadowCoord.x, 0);
	shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.x));
	shadowCoord.w = max(shadowCoord.w, step(shadowCoord.y, 0));
	shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.y));
	shadowCoord.w = max(shadowCoord.w, step(shadowCoord.z, 0));
	shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.z));
	float depth = DecodeFloatRGBA(tex2D(_actShadowMap, shadowCoord.xy));
	float shade = max(step(shadowCoord.z - _bias, depth), _strength);
	shade = max(shade, shadowCoord.w);
	return shade;
}
inline float FragPCF2x2(float4 shadowCoord,sampler2D _actShadowMap,fixed _bias,fixed _strength,fixed _texmapScale)
{
	float sum = 0;
	float x,y;
	for (y = -0.5; y <= 0.5; y += 1.0)
	  for (x = -0.5; x <= 0.5; x += 1.0)
	  {
			float4 offset_lookup = tex2D(_actShadowMap, shadowCoord.xy + float2(x,y) * _texmapScale);
	  		float depth = DecodeFloatRGBA(offset_lookup);
			float shade =  max(step(shadowCoord.z - _bias, depth), _strength);
			shadowCoord.w = step(shadowCoord.x, 0);
			shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.x));
			shadowCoord.w = max(shadowCoord.w, step(shadowCoord.y, 0));
			shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.y));
			shadowCoord.w = max(shadowCoord.w, step(shadowCoord.z, 0));
			shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.z));
			shade = max(shade, shadowCoord.w);
			sum += shade;
	  }
	sum = sum / 4.0;
	return sum;
}

inline float FragPCF4Samples(float4 shadowCoord,sampler2D _actShadowMap,fixed _bias,fixed _strength,fixed _texmapScale)
{
	float sum = 0;
	float2 offset = (float)(frac(shadowCoord.xy * 0.5) > 0.25);  
	offset.y += offset.x; 

	float4 offset_lookup = tex2D(_actShadowMap, shadowCoord.xy + offset + float2(-1.5, 0.5) * _texmapScale);
	float depth = DecodeFloatRGBA(offset_lookup);
	sum += max(step(shadowCoord.z - _bias, depth), _strength) * 0.25;

	offset_lookup = tex2D(_actShadowMap, shadowCoord.xy + offset + float2(0.5, 0.5) * _texmapScale);
	depth = DecodeFloatRGBA(offset_lookup);
	sum += max(step(shadowCoord.z - _bias, depth), _strength) * 0.25;

	offset_lookup = tex2D(_actShadowMap, shadowCoord.xy + offset + float2(-1.5, -1.5) * _texmapScale);
	depth = DecodeFloatRGBA(offset_lookup);
	sum += max(step(shadowCoord.z - _bias, depth), _strength) * 0.25;

	offset_lookup = tex2D(_actShadowMap, shadowCoord.xy + offset + float2(0.5, -1.5) * _texmapScale);
	depth = DecodeFloatRGBA(offset_lookup);
	sum += max(step(shadowCoord.z - _bias, depth), _strength) * 0.25;

	shadowCoord.w = step(shadowCoord.x, 0);
	shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.x));
	shadowCoord.w = max(shadowCoord.w, step(shadowCoord.y, 0));
	shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.y));
	shadowCoord.w = max(shadowCoord.w, step(shadowCoord.z, 0));
	shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.z)); 
	sum = max(sum, shadowCoord.w);
	
	return sum;
}

inline float FragPCF4x4(float4 shadowCoord,sampler2D _actShadowMap,fixed _bias,fixed _strength,fixed _texmapScale)
{
	float sum = 0;
	float x,y;
	for (y = -1.5; y <= 1.5; y += 1.0)
	  for (x = -1.5; x <= 1.5; x += 1.0)
	  {
			float4 offset_lookup = tex2D(_actShadowMap, shadowCoord.xy + float2(x,y) * _texmapScale);
			float depth = DecodeFloatRGBA(offset_lookup);
			float shade =  max(step(shadowCoord.z - _bias, depth), _strength);
			shadowCoord.w = step(shadowCoord.x, 0);
			shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.x));
			shadowCoord.w = max(shadowCoord.w, step(shadowCoord.y, 0));
			shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.y));
			shadowCoord.w = max(shadowCoord.w, step(shadowCoord.z, 0));
			shadowCoord.w = max(shadowCoord.w, step(1, shadowCoord.z));
			shade = max(shade, shadowCoord.w);
			sum += shade;
	  }
	sum = sum / 16.0;
	return sum;
}

#endif