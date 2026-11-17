// DUAL DUEL - 씬 전환 글리치
// 캡처한 화면을 행 단위로 잘라 좌우로 밀고(지직), 노이즈 줄/끊긴 줄을 섞는다.
// SceneTransition.cs 가 _Progress / _Blackout / _Fade / _Step 을 매 프레임 넣어준다.
// 이 오버레이는 CRT 후처리 뒤에 그려지므로 RGB 분리 대신 초록 계열 밝기 잔상과 노이즈만 쓴다.
Shader "DUAL DUEL/Glitch Transition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Captured Screen", 2D) = "black" {}
        _Progress ("Glitch Amount", Range(0, 1)) = 0
        _Blackout ("Dropped Rows", Range(0, 1)) = 0
        _Fade ("Fade To Black", Range(0, 1)) = 0
        _Rows ("Row Count", Float) = 120
        _BandSize ("Rows Per Band", Float) = 6
        _MaxShift ("Max Shift (x screen width)", Range(0, 0.5)) = 0.18
        _PixelStep ("Shift Snap (px)", Float) = 4
        _NoiseRows ("Noise Rows", Range(0, 1)) = 0.25
        _Ghost ("Ghost Offset (x screen width)", Range(0, 0.05)) = 0.012
        _Tint ("Phosphor Tint", Color) = (0.54, 1, 0.65, 1)
        _Step ("Time Step (script)", Float) = 0
        _ScreenW ("Screen Width (script)", Float) = 1920
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "False" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Progress, _Blackout, _Fade, _Rows, _BandSize, _MaxShift, _PixelStep, _NoiseRows, _Ghost, _Step, _ScreenW;
            float4 _Tint;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            // 0~1 의사 난수 (프레임마다 _Step 이 바뀌어 지직거림)
            float hash (float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float p = _Progress;
                float t = _Step;
                float row = floor(uv.y * _Rows);
                float band = floor(row / _BandSize);

                // 1) 굵은 띠가 크게 밀리고, 2) 그 안의 행들이 잘게 떨린다
                float hb = hash(float2(band, t));
                float shift = 0;
                if (hb < p * 0.85) shift += (hash(float2(band + 17.0, t)) - 0.5) * 2.0 * _MaxShift * p;
                if (hash(float2(row, t + 3.1)) < p * 0.5) shift += (hash(float2(row + 5.0, t)) - 0.5) * _MaxShift * 0.35 * p;
                // 픽셀 단위로 딱딱 끊기게
                shift = round(shift * _ScreenW / _PixelStep) * _PixelStep / _ScreenW;

                float2 suv = float2(uv.x + shift, uv.y);
                float inside = step(0.0, suv.x) * step(suv.x, 1.0);
                float3 col = tex2D(_MainTex, suv).rgb * inside;

                // 신호가 겹치는 잔상 (색을 가르지 않고 밝기로만)
                float gdir = hb < p ? 1.0 : -1.0;
                float3 ghost = tex2D(_MainTex, suv + float2(_Ghost * p * gdir, 0)).rgb;
                col = max(col, ghost * 0.45 * p * inside);

                // 밀린 띠의 윗줄이 밝게 번쩍
                if (hb < p * 0.3 && fmod(row, _BandSize) < 1.0) col = max(col, _Tint.rgb * 0.75 * p);

                // 노이즈 줄
                if (hash(float2(row + 29.0, t)) < _NoiseRows * p)
                {
                    float n = hash(float2(floor(uv.x * _ScreenW / _PixelStep), row + t * 7.0));
                    col = lerp(col, _Tint.rgb * n, 0.85);
                }

                // 끊긴 줄 (검게)
                if (hash(float2(row + 41.0, t)) < _Blackout) col = 0;

                col *= 1.0 - _Fade;
                return fixed4(col, i.color.a);
            }
            ENDCG
        }
    }
}
