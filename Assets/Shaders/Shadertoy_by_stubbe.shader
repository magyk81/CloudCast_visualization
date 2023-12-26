Shader "Unlit/Shadertoy_by_stubbe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FluffHeight ("Fluff Height", Float) = 4.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct vertInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct fragInput
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FluffHeight;

            fragInput vert (vertInput input)
            {
                fragInput output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            float4 frag (fragInput input) : SV_Target
            {
                float4 d = float4(0.8, 0, input.vertex.xy / _ScreenParams.y - 0.8);
                float4 c = float4(0.6, 0.7, d.xy);

                float4 output = c - d.w;

                for (float t = 200.0 + sin(dot(input.vertex, input.vertex)); t > 0; t--) {
                    float4 p = 0.05 * t * d;
                    p.xz += _Time.y;

                    float s = 2.0;
                    float teaSum = 3.0;
                    for (int i = 0; i < 4; i++) {
                        float2 texSampleCoord = (s * p.zw + ceil(s * p.x)) / 200;
                        teaSum -= tex2D(_MainTex, texSampleCoord).y / s * _FluffHeight;
                        s += s;
                    }
                    float f = p.w + teaSum;

                    if (f < 0) {
                        output += (output - 1.0 - f * c.zyxw) * f * 0.4;
                    }
                }

                // sample the texture
                //float output = tex2D(_MainTex, input.uv);

                return output;
            }
            ENDCG
        }
    }
}
