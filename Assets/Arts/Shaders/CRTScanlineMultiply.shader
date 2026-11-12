// DUAL DUEL - CRT 스캔라인 (2x 곱하기 블렌딩)
// 출력 0.5 = 변화 없음, 0.5 미만 = 어둡게, 0.5 초과 = 밝게
// -> 어두운 줄에서 뺀 밝기만큼 밝은 줄에 더해서 화면 평균 밝기를 유지함
Shader "DUAL DUEL/UI/CRT Scanline Multiply"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture (사용 안 함)", 2D) = "white" {}

        [Header(Scanline)]
        _Intensity      ("줄 진하기", Range(0, 1)) = 0.7
        _LineSpacing    ("줄 간격 (px)", Range(2, 16)) = 4
        _LineThickness  ("어두운 줄 두께 비율", Range(0.05, 0.6)) = 0.4
        _Softness       ("줄 경계 부드러움", Range(0, 1)) = 0.4
        _Compensation   ("밝기 보정", Range(0, 1)) = 1

        [Header(Scroll)]
        _ScrollSpeed    ("위로 흐르는 속도 (px per sec)", Float) = 12
        [Toggle] _PixelSnap ("픽셀 단위로 끊어서 이동", Float) = 1

        [Header(Rolling Bar)]
        _RollIntensity  ("롤링 밴드 밝기", Range(0, 0.5)) = 0.08
        _RollSpeed      ("롤링 밴드 속도", Float) = 0.12
        _RollWidth      ("롤링 밴드 두께", Range(0.02, 0.5)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        // 결과 = src * dst + dst * src = 2 * src * dst
        Blend DstColor SrcColor
        ColorMask RGB

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            float _Intensity, _LineSpacing, _LineThickness, _Softness, _Compensation;
            float _ScrollSpeed, _PixelSnap;
            float _RollIntensity, _RollSpeed, _RollWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 화면 픽셀 좌표 (아래 = 0). 풀스크린 RawImage의 uv 사용 -> 플랫폼별 y 뒤집힘 없음
                float y = i.uv.y * _ScreenParams.y;

                // 위로 흐르는 스크롤. 픽셀아트는 서브픽셀 이동 시 줄이 번쩍여 보이므로 정수 단위로 끊음
                float offset = _Time.y * _ScrollSpeed;
                offset = _PixelSnap > 0.5 ? floor(offset) : offset;

                float phase = frac((y - offset) / _LineSpacing);

                // p = 1 인 곳이 어두운 줄. 평균값은 대략 _LineThickness
                float t = _LineThickness;
                float s = _Softness * 0.5 * min(t, 1 - t) + 1e-4;
                float p = 1 - smoothstep(t - s, t + s, phase);

                float mask = 1 - _Intensity * p;

                // 밝기 보정: 줄로 잃은 평균 밝기만큼 전체를 끌어올림 (평균 = 1 유지)
                float gain = 1.0 / max(1 - _Intensity * t, 0.5);
                mask *= lerp(1.0, gain, _Compensation);

                // 화면을 천천히 타고 올라가는 밝은 띠
                float band = frac(_Time.y * _RollSpeed);
                float d = abs(i.uv.y - band);
                d = min(d, 1 - d);
                mask *= 1 + _RollIntensity * exp(-(d * d) / (_RollWidth * _RollWidth));

                // 2x 블렌딩이라 0.5를 곱함. mask 1 = 원본 그대로
                float c = saturate(mask * 0.5);
                return fixed4(c, c, c, 1);
            }
            ENDCG
        }
    }
}
