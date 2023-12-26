// Ported from shader "Clouds" by Inigo Quilez from Shadertoy.
// This is not done being ported due to heavy restrictions put out by the author.

Shader "Unlit/Shadertoy_by_iq" {

    Properties {
        _MainTex ("Texture", 2D) = "red" {}
        _CameraControlX ("Camera Control X", Range(0.0, 1.0)) = 0.0
        _CameraControlY ("Camera Control Y", Range(0.0, 1.0)) = 0.0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // In shadertoy, ro and ta has "in" prepended, don't know why
            float3x3 setCamera(float3 ro, float3 ta, float cr )
            {
                float3 cw = normalize(ta - ro);
                float3 cp = float3(sin(cr), cos(cr), 0.0);
                float3 cu = normalize(cross(cw,cp));
                float3 cv = normalize(cross(cu,cw));
                return float3x3(cu, cv, cw);
            }

            struct vertInput {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct fragInput {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float1 _CameraControlX;
            float1 _CameraControlY;

            fragInput vert (vertInput input) {
                fragInput vertOutput;
                vertOutput.vertex = UnityObjectToClipPos(input.vertex);
                vertOutput.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return vertOutput;
            }

            fixed4 frag (fragInput input) : SV_Target {
                // sample the texture
                fixed4 col = tex2D(_MainTex, input.uv);

                float2 p = (mul(2.0, input.vertex) - _ScreenParams.xy) / _ScreenParams.y;
                float2 m = float2(_CameraControlX, _CameraControlY)      / _ScreenParams.xy;

                // camera
                float3 ro = mul(4.0, normalize(float3(sin(mul(3.0, m.x)), mul(0.8, m.y), cos(mul(3.0, m.x))))) - float3(0.0, 1.0, 0.0);

                return col;
            }
            ENDCG
        }
    }
}
