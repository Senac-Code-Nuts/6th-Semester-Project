Shader "Custom/2D/UnderwaterCaustics"
{
    Properties
    {
        [HDR] _Color ("Caustics Color (HDR)", Color) = (0.4, 0.9, 1.0, 0.4)
        _Scale ("Scale", Float) = 4.0
        _Speed ("Speed", Float) = 0.8
        _Sharpness ("Sharpness", Range(1.0, 8.0)) = 3.5
        _Intensity ("Intensity", Float) = 1.2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            Name "Forward"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Scale;
                float _Speed;
                float _Sharpness;
                float _Intensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float2 Hash2D(float2 p)
            {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }

            float Voronoi(float2 uv, float timeVal)
            {
                float2 g = floor(uv);
                float2 f = frac(uv);
                float minD = 1.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 lattice = float2(x, y);
                        float2 offset = Hash2D(g + lattice);
                        offset = 0.5 + 0.4 * sin(timeVal + 6.2831 * offset);
                        float d = distance(lattice + offset, f);
                        minD = min(minD, d);
                    }
                }
                return minD;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv * _Scale;
                float t = _Time.y * _Speed;

                float c1 = Voronoi(uv + float2(t * 0.2, t * 0.1), t * 1.3);
                float c2 = Voronoi(uv * 1.4 - float2(t * 0.15, -t * 0.25), t * 1.7 + 2.0);

                float caustic = min(c1, c2);
                caustic = pow(saturate(1.0 - caustic), _Sharpness);

                float edgeFade = smoothstep(0.0, 0.1, input.uv.x) * smoothstep(1.0, 0.9, input.uv.x) *
                                smoothstep(0.0, 0.1, input.uv.y) * smoothstep(1.0, 0.9, input.uv.y);

                half4 col = _Color * caustic * _Intensity * edgeFade;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
