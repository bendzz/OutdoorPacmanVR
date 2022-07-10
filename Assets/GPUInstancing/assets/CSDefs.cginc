#define ThreadCount1D 64



struct terrainCell
{
	float height;
	float3 info;	// debugging
};

struct bot
{
	float3 pos;
             //quaternion rot;
	float3 scale;
	
	float3 info;
};

struct Submesh
{
	int verticesStart;
	int verticesCount;

	int trianglesStart;
	int trianglesCount;
};



// https://stackoverflow.com/questions/15628039/simplex-noise-shader
// hash based 3d value noise
// function taken from https://www.shadertoy.com/view/XslGRr
// Created by inigo quilez - iq/2013
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

// ported from GLSL to HLSL

float hash(float n)
{
	return frac(sin(n) * 43758.5453);
}

float noise(float3 x)
{
    // The noise function returns a value in the range -1.0f -> 1.0f

	float3 p = floor(x);
	float3 f = frac(x);

	f = f * f * (3.0 - 2.0 * f);
	float n = p.x + p.y * 57.0 + 113.0 * p.z;

	return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                   lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
               lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                   lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
}



// Compute barycentric coordinates (u, v, w) for
// point p with respect to triangle (a, b, c)
float3 barycentricCoordinates(float3 p, float3 a, float3 b, float3 c)
{
// https://gamedev.stackexchange.com/questions/23743/whats-the-most-efficient-way-to-find-barycentric-coordinates
	float3 bary = (float3) 0;
	// float& u, float& v, float& w
	
	float3 v0 = b - a;
	float3 v1 = c - a;
	float3 v2 = p - a;
	float d00 = dot(v0, v0);
	float d01 = dot(v0, v1);
	float d11 = dot(v1, v1);
	float d20 = dot(v2, v0);
	float d21 = dot(v2, v1);
	float denom = d00 * d11 - d01 * d01;
	bary.x = (d11 * d20 - d01 * d21) / denom;
	bary.y = (d00 * d21 - d01 * d20) / denom;
	bary.z = 1 - bary.x - bary.y;
	
	return bary.zxy;
	//return bary;	// wait does the stackexchange code output stuff in the wrong order by default???
};


float3 getNormal(float3 v1, float3 v2, float3 v3)
{
	float3 edge1 = v2 - v1;
	float3 edge2 = v3 - v1;
	float3 normal = cross(edge1.xyz, edge2.xyz);
	return normalize(normal);
}