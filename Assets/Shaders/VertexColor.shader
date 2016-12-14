Shader "Custom/VertexColor" {
	Properties{
		point_size("Point Size", Float) = 5.0
	}
    SubShader {
    Pass {

                 
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

		#include "UnityCG.cginc"
  
		float point_size;

        struct VertexInput {
            float4 v : POSITION;
            float4 color: COLOR;
        };
         
        struct VertexOutput {
            float4 pos : SV_POSITION;
            float4 col : COLOR;
			float4 size : PSIZE;
        };
         
        VertexOutput vert(VertexInput v) {
         
            VertexOutput o;
            o.pos = mul(UNITY_MATRIX_MVP, v.v);
			///o.pos[0] += 1;
			//o.pos[1] -= 1;
            o.col = v.color;
			o.size = point_size;
            return o;
        }
         
        float4 frag(VertexOutput o) : COLOR {
            return o.col;
        }
 
        ENDCG
        } 
    }
 
}
