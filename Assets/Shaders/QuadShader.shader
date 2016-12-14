Shader "Custom/QuadShader"
{
	Properties
	{
		_PointSize("Point Size", Float) = 1.0
		_LightingAffect("LightingAffect", Float) = 0.5
	}

	SubShader
	{
		LOD 0
	Tags{
		"RenderType" = "Opaque"
	}
	Pass
	{ 
		Lighting On
		CGPROGRAM

#pragma only_renderers d3d11
#pragma target 4.0

#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "HSB.cginc"

#pragma vertex   myVertexShader
#pragma geometry myGeometryShader
#pragma fragment myFragmentShader

#define TAM 6

		struct vIn // Into the vertex shader
	{
		float4 vertex : POSITION;
		float4 color  : COLOR0;
		float3 normal : NORMAL;
	};

	struct gIn // OUT vertex shader, IN geometry shader
	{
		float4 pos : SV_POSITION;
		float4 col : COLOR0;
		float3 normal : NORMAL;
	};

	struct v2f // OUT geometry shader, IN fragment shader 
	{
		float4 pos           : SV_POSITION;
		float2 uv_MainTex : TEXCOORD0;
		float4 col : COLOR0;
		float3 normal : NORMAL;
	};

	float4       _MainTex_ST;
	sampler2D _MainTex;
	float     _PointSize;
	float _LightingAffect;

	// ----------------------------------------------------
	gIn myVertexShader(vIn v)
	{
		gIn o; // Out here, into geometry shader
			   // Passing on color to next shader (using .r/.g there as tile coordinate)
		o.col = v.color;
		// Passing on center vertex (tile to be built by geometry shader from it later)
		o.pos = v.vertex;
		o.normal = v.normal;

		return o;
	}

	// ----------------------------------------------------

	[maxvertexcount(TAM)]
	// ----------------------------------------------------
	// Using "point" type as input, not "triangle"
	void myGeometryShader(point gIn vert[1], inout TriangleStream<v2f> triStream)
	{
		float4 pos = mul(UNITY_MATRIX_MVP, vert[0].pos);
		float f = _PointSize * pos.z ; //half size

		const float4 vc[TAM] = {

			float4(-f,  f, -f, 0.0f), float4(f,  f, -f, 0.0f), float4(f, -f, -f, 0.0f),     //Front
			float4(f, -f, -f, 0.0f), float4(-f, -f, -f, 0.0f), float4(-f,  f, -f, 0.0f)     //Front

		};


		const float2 UV1[TAM] = { 


			float2(0.0f,    0.0f), float2(1.0f,    0.0f), float2(1.0f,    0.0f),
			float2(1.0f,    0.0f), float2(1.0f,    0.0f), float2(1.0f,    0.0f)


		};

		const int TRI_STRIP[TAM] = { 

			0,1,2, 3,4,5

		};

		v2f v[TAM];
		int i;


		// Assign new vertices positions 
		for (i = 0; i<TAM; i++) { 
			v[i].pos = vert[0].pos + vc[i]; 
			v[i].col = vert[0].col; 
			v[i].normal = vert[0].normal;
		}

		// Assign UV values
		for (i = 0; i<TAM; i++) v[i].uv_MainTex = TRANSFORM_TEX(UV1[i],_MainTex);

		// Position in view space

		float4 col = vert[0].col;
			//applyHSBEffect(vert[0].col, float4(0, 0.5, 0.5, 2.0));
		float4 lighting = float4(ShadeVertexLights(vert[0].pos, vert[0].normal), 1.0);

		col = col * (1.0 - _LightingAffect) + (col * lighting) *_LightingAffect + lighting * _LightingAffect * 0.5f;
		for (i = 0; i<TAM; i++) 
		{ 
			v[i].pos = mul(UNITY_MATRIX_MVP, v[i].pos);

			


			v[i].col = col;
		}

		// Build the cube tile by submitting triangle strip vertices
		for (i = 0; i<TAM / 3; i++)
		{
			triStream.Append(v[TRI_STRIP[i * 3 + 0]]);
			triStream.Append(v[TRI_STRIP[i * 3 + 1]]);
			triStream.Append(v[TRI_STRIP[i * 3 + 2]]);

			triStream.RestartStrip();
		}
	}

	// ----------------------------------------------------
	float4 myFragmentShader(v2f IN) : COLOR
	{
		//return float4(IN.normal.x,IN.normal.y, IN.normal.z, 1.0);
		return IN.col;
	}

		ENDCG
	}






	}
}