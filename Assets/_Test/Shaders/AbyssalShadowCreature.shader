Shader "Custom/2D/AbyssalShadowCreature"
{
    Properties
    {
        _ShadowColor    ("Shadow Tint", Color) = (0.02, 0.04, 0.06, 0.85)
        _Speed          ("Swim Speed", Float) = 0.4
        _TentacleWiggle ("Wiggle Speed", Float) = 2.0
        _HeadScale      ("Size / Scale", Float) = 1.0
        _EyeGlowColor   ("Eye Glow (HDR)", Color) = (1.0, 0.3, 0.05, 1.0)
        _EyeIntensity   ("Eye Intensity", Float) = 3.0
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowColor;
                float  _Speed;
                float  _TentacleWiggle;
                float  _HeadScale;
                float4 _EyeGlowColor;
                float  _EyeIntensity;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            // ---------- hash / helpers ----------
            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float EllipseDist(float2 p, float2 c, float2 r)
            {
                float2 d = (p - c) / r;
                return length(d) - 1.0;
            }

            float SmoothMin(float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (b - a) / k);
                return lerp(b, a, h) - k * h * (1.0 - h);
            }

            // Criatura em espaço local "deitado":
            //   p.y > 0  = direção da cabeça (tela direita)
            //   p.y < 0  = direção da cauda (tela esquerda)
            //   p.x      = eixo vertical na tela
            float CreatureSDF(float2 p, float t, float seed, float tailLen)
            {
                float phase = seed * 6.2831853;

                // leve ondulação lateral do corpo
                p.x += sin(p.y * 2.5 + t * 1.2 + phase) * 0.05;

                // cabeça
                float head = EllipseDist(p, float2(0.0, 0.15), float2(0.30, 0.28));

                // tentáculos / cauda
                float tent = 1e9;
                for (int i = 0; i < 5; i++)
                {
                    float fi     = (float)i - 2.0;
                    float baseX  = fi * 0.10;
                    float tPhase = phase + fi * 1.35;

                    float yTop  = 0.05;
                    float yBot  = -tailLen;
                    float yNorm = saturate((yTop - p.y) / (yTop - yBot));

                    float wig   = sin(p.y * 3.0 + tPhase + t * _TentacleWiggle) * 0.16 * yNorm;
                    float cx    = baseX + wig;
                    float halfW = lerp(0.055, 0.0025, yNorm);
                    float d     = abs(p.x - cx) - halfW;
                    float clipMask = max(p.y - yTop, yBot - p.y);

                    tent = SmoothMin(tent, max(d, clipMask), 0.04);
                }

                return SmoothMin(head, tent, 0.05);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv     = IN.uv;
                float  aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float  t      = _Time.y;

                // Uma travessia por ciclo. cycle em segundos = 1 / _Speed
                float cycle    = 1.0 / max(_Speed, 0.001);
                float tt       = t / cycle;
                float passIdx  = floor(tt);
                float passFrac = frac(tt);

                // X vai de fora-esquerda até fora-direita
                float xPos = lerp(-0.5, 1.5, passFrac);

                // Seeds por passagem → variações a cada travessia
                float sizeSeed  = hash11(passIdx * 7.31 + 1.0);
                float tailSeed  = hash11(passIdx * 3.11 + 4.0);
                float ySeed     = hash11(passIdx * 2.71 + 5.0);
                float phaseSeed = hash11(passIdx * 5.17 + 2.0);

                float sizeMul = lerp(0.55, 1.55, sizeSeed);
                float tailMul = lerp(0.90, 1.40, tailSeed);
                float bandY   = 0.20 + ySeed * 0.55;

                float bandSize = 0.25 * _HeadScale * sizeMul;

                // ---------- espaço local ----------
                float2 p;
                p.x = (uv.x - xPos) * aspect;
                p.y = (uv.y - bandY);

                p.y -= sin(t * 0.9 + phaseSeed * 6.28) * 0.10;

                p /= bandSize;

                // gira 90° → deitado, cabeça pra direita, cauda pra esquerda
                p = float2(p.y, p.x);

                // corpo
                float tailLen = 2.0 * tailMul;
                float d       = CreatureSDF(p, t, phaseSeed, tailLen);
                float aa      = max(fwidth(d), 0.001) * 1.2;
                float cov     = 1.0 - smoothstep(-aa, aa, d);

                // A criatura aparece/some suavemente nas bordas da tela,
                // evitando que a cauda "pisce" do nada quando passIdx muda.
                float fadeLen  = 0.18;
                float edgeFade = smoothstep(0.0, fadeLen, passFrac)
                               * (1.0 - smoothstep(1.0 - fadeLen, 1.0, passFrac));
                cov *= edgeFade;

                // olhos
                float eL = EllipseDist(p, float2(-0.10, 0.20), float2(0.045, 0.055));
                float eR = EllipseDist(p, float2( 0.10, 0.20), float2(0.045, 0.055));
                float eyeDist = min(eL, eR);
                float eyeMask = (1.0 - smoothstep(-0.04, 0.06, eyeDist)) * cov;
                float eyeGlow = exp(-max(eyeDist, 0.0) * 8.0) * cov;

                // composição
                float3 rgb   = _ShadowColor.rgb * cov
                             + _EyeGlowColor.rgb * _EyeIntensity * (eyeMask + eyeGlow * 0.4);
                float  alpha = saturate(cov * _ShadowColor.a + eyeMask);

                rgb   *= IN.color.rgb;
                alpha *= IN.color.a;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}