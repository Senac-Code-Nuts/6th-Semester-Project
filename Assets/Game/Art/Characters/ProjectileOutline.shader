Shader "PiGame/Projectile Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Inner outline", Color) = (0.025, 0.025, 0.04, 1)
        _OuterColor ("Outer outline", Color) = (0.92, 0.94, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                half4 _OuterColor;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half NearbyAlpha(float2 uv, float width)
            {
                float2 step = _MainTex_TexelSize.xy * width;
                half alpha = 0;
                [unroll] for (int x = -1; x <= 1; x++)
                    [unroll] for (int y = -1; y <= 1; y++)
                        alpha = max(alpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(x,y) * step).a);
                return alpha;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half inner = NearbyAlpha(input.uv, 1);
                half outer = NearbyAlpha(input.uv, 2);
                half4 result = inner > 0.01 ? _OutlineColor : _OuterColor;
                result.rgb = lerp(result.rgb, sprite.rgb, sprite.a);
                result.a = max(sprite.a, outer) * input.color.a;
                return result;
            }
            ENDHLSL
        }
    }
}
