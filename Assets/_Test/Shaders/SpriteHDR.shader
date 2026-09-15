Shader "Custom/2D/SpriteHDR"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Tint / Emission Color (HDR)", Color) = (1, 1, 1, 1)
        _Intensity ("Emission Multiplier", Float) = 1.0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float4 _RendererColor;
                float4 _Flip;
            CBUFFER_END

            float4 PixelSnapFunction(float4 posCS)
            {
                float2 pixelPos = round((posCS.xy / posCS.w) * _ScreenParams.xy * 0.5) / (_ScreenParams.xy * 0.5);
                posCS.xy = pixelPos * posCS.w;
                return posCS;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 pos = input.positionOS;
                pos.xy *= _Flip.xy;

                output.positionCS = TransformObjectToHClip(pos.xyz);
                output.uv = input.uv;
                output.color = input.color * _RendererColor;

                #if defined(PIXELSNAP_ON)
                output.positionCS = PixelSnapFunction(output.positionCS);
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 finalColor = texColor * input.color * _Color * _Intensity;
                finalColor.a = texColor.a * input.color.a * _Color.a;
                return finalColor;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
