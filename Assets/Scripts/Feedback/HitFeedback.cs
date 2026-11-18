using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum HitKind { Damage, Block, Positive, Buff, Debuff }

/// <summary>
/// DUAL DUEL 타격 연출 보강. 기존 DOTweenManager / BattleVFXPlayer / ShakeByDamage 가 하지 않는 것만 담당.
///   Anticipate()  카드가 젖혀질 때 화면이 숨을 들이쉬듯 살짝 어두워짐 (노출 ↓, 비네트 ↑)
///   Beam()        카드에서 대상 초상화까지 빛줄기 (원인 -> 결과 연결)
///   Impact()      맞는 순간 CRT 볼륨 펀치 (노출 플래시, 색수차, 렌즈 왜곡) + 히트스톱
///   PlayChargedCast()  실제 피해가 큰 공격: 차징(젖힘 + 떨림 + 원형 빛) -> 굵게 머무는 빛줄기 + 스파크 + 착탄 빛
///                      (화면 전체 섬광은 쓰지 않음: 매 턴 보면 눈이 피로하므로 광선/스파크/착탄 빛으로만 세기를 표현)
///   SparkBurst()  스파크 튀기기 (큰 피해 숫자 등에서도 사용)
/// 볼륨 펀치는 런타임 전용 Volume 을 하나 더 만들어 weight 로 섞으므로
/// BattleScene_CRT_Profile 과 CRTFlicker 는 건드리지 않음.
/// 씬의 아무 오브젝트에 붙이면 끝 (BattleManager 와 같은 오브젝트 권장).
/// </summary>
public class HitFeedback : MonoBehaviour
{
    public static HitFeedback Instance { get; private set; }

    [Serializable]
    public struct Look
    {
        [Tooltip("Post Exposure 목표값 (기본 프로필 0.2 에서 이 값으로 섞임)")] public float exposure;
        [Tooltip("Chromatic Aberration 목표값 (0 이면 건드리지 않음)")] public float chroma;
        [Tooltip("Bloom Intensity 목표값 (0 이면 건드리지 않음, 기본 0.7)")] public float bloom;
        [Tooltip("Lens Distortion (음수 = 빨려듦, 양수 = 밀어냄, 0 이면 건드리지 않음)")] public float lens;
        [Tooltip("Vignette Intensity 목표값 (0 이면 건드리지 않음, 기본 0.2)")] public float vignette;
        [Tooltip("최대치까지 오르는 시간")] public float attack;
        [Tooltip("최대치에서 0 으로 돌아오는 시간")] public float release;
        [Tooltip("돌아오는 동안 프레임마다 깜빡임 (부정 효과용)")] public bool flicker;
    }

    [Header("피해량 기준 (이 값 이상이면 최대 강도)")]
    public int bigHit = 10;

    [Header("예고: 카드가 젖혀지는 동안 화면이 살짝 어두워짐")]
    public bool useAnticipation = true;
    public Look anticipation = new Look { exposure = -0.3f, vignette = 0.42f, attack = 0.1f, release = 0.25f };
    [Tooltip("Impact 가 오지 않을 때 예고가 자동으로 풀리기까지의 시간")] public float anticipationHold = 0.25f;

    [Header("임팩트: CRT 볼륨 펀치")]
    public bool useVolumePunch = true;
    public Look damage = new Look { exposure = 1.1f, chroma = 0.85f, bloom = 2.4f, lens = -0.28f, attack = 0f, release = 0.22f };
    public Look block = new Look { exposure = 0.9f, chroma = 0.3f, bloom = 2.2f, lens = 0.16f, attack = 0f, release = 0.2f };
    public Look buff = new Look { exposure = 0.6f, bloom = 1.8f, attack = 0.06f, release = 0.35f };
    public Look debuff = new Look { exposure = -0.5f, chroma = 1f, lens = -0.1f, attack = 0f, release = 0.3f, flicker = true };
    public int volumePriority = 100;

    [Header("히트스톱 (Damage / Block 만)")]
    public bool useHitStop = true;
    public float hitStopMin = 0.03f;
    public float hitStopMax = 0.09f;
    [Tooltip("멈춤 동안의 timeScale. 0 이면 파티클이 첫 프레임을 못 그릴 수 있어 아주 작은 값 권장")]
    [Range(0f, 0.2f)] public float hitStopTimeScale = 0.02f;

    [Header("빛줄기")]
    public bool useBeam = true;
    [Tooltip("두께 (월드 단위). 0.06 = 원본 3px")] public float beamWidth = 0.06f;
    [Range(0.05f, 1f)] public float beamTail = 0.35f;
    [ColorUsage(false, true)] public Color beamColor = new Color(1.4f, 2.2f, 1.5f, 1f);
    [Tooltip("HUD 캔버스(100 이상) 위, FX(150) 아래, 스캔라인(200) 아래")] public int beamSortingOrder = 149;
    [Tooltip("비워두면 Sprites/Default 로 자동 생성")] public Material beamMaterial;

    [Header("차징: 실제 피해가 큰 공격 (x = 기준 피해일 때, y = 최대 피해 이상일 때)")]
    public bool useCharge = true;
    [Tooltip("실제 체력 피해가 이 값 이상이면 차징 시전")] public int chargeThreshold = 15;
    [Tooltip("실제 체력 피해가 이 값 이상이면 모든 차징 연출이 최대")] public int chargeMaxValue = 35;
    [Tooltip("차징 시간 (초)")] public Vector2 chargeTime = new Vector2(1f, 3f);
    [Tooltip("뒤로 젖혀지는 각도 (도)")] public Vector2 chargeTilt = new Vector2(10f, 18f);
    [Tooltip("뒤로 물러나는 거리 (월드 단위)")] public Vector2 chargeBack = new Vector2(0.15f, 0.3f);
    [Tooltip("좌우 떨림 폭 (월드 단위)")] public Vector2 chargeShake = new Vector2(0.015f, 0.05f);
    [Tooltip("좌우 떨림 속도 (초당 횟수)")] public Vector2 chargeShakeHz = new Vector2(24f, 36f);
    [Tooltip("카드 위 원형 빛 지름 (월드 단위, 카드 폭 1.6 의 절반 = 0.8)")] public Vector2 glowSize = new Vector2(0.8f, 1.6f);
    [Tooltip("빛줄기 두께 배율 (beamWidth 에 곱함)")] public Vector2 beamThickness = new Vector2(3f, 7f);
    [Tooltip("빛줄기 스파크 수 (날아가는 동안 + 머무는 동안, 도착할 때 1/3 만큼 더 터짐)")] public Vector2 beamSparks = new Vector2(40f, 140f);
    [Tooltip("도착한 빛줄기가 떨리며 머무는 시간 (초)")] public Vector2 beamLinger = new Vector2(0.15f, 0.45f);
    [Tooltip("착탄 지점(대상 초상화)에 터지는 원형 빛 지름 (월드 단위)")] public Vector2 impactGlow = new Vector2(1.2f, 2.6f);
    [ColorUsage(false, true)] public Color glowColor = new Color(1.6f, 2.4f, 1.7f, 1f);
    [Tooltip("원형 빛 정렬 순서 (빛줄기 149 바로 아래)")] public int glowSortingOrder = 148;

    /// <summary>밸류에 따라 계산된 차징 세기</summary>
    public struct Charge
    {
        public bool active;
        public float k;            // 0 (기준 밸류) ~ 1 (최대 밸류)
        public float time, tilt, back, shake, shakeHz, glow, thickness, linger, impact;
        public int sparks;
    }

    Volume _volume;
    ColorAdjustments _color;
    ChromaticAberration _chroma;
    Bloom _bloom;
    LensDistortion _lens;
    Vignette _vignette;

    float _envelope;     // 0~1, DOTween 이 움직이고 LateUpdate 에서 weight 로 반영
    bool _flicker;
    Sequence _envTween;

    int _stops;
    float _savedTimeScale = 1f;

    // 스파크 풀 (월드 스페이스 픽셀 사각형)
    struct Spark { public Transform tr; public SpriteRenderer sr; public Vector3 vel; public float age, life; public bool alive; }
    readonly List<Spark> _sparks = new List<Spark>();
    const int MaxSparks = 256;
    Sprite _pixelSprite, _glowSprite;
    Material _glowMaterial;

    // ------------------------------------------------------------------ setup

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildVolume();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        _envTween?.Kill();
        if (_stops > 0) { Time.timeScale = _savedTimeScale; _stops = 0; }
    }

    void OnDisable()
    {
        // 중간에 꺼져도 화면/시간이 이상한 상태로 남지 않게
        _envTween?.Kill();
        _envelope = 0f;
        if (_volume != null) _volume.weight = 0f;
        if (_stops > 0) { Time.timeScale = _savedTimeScale; _stops = 0; }
    }

    void Update()
    {
        // 스파크: 중력 + 감속, 흰색에서 초록으로 식으며 끝에 깜빡임 (히트스톱 중엔 같이 멈춤)
        float dt = Time.deltaTime;
        for (int i = 0; i < _sparks.Count; i++)
        {
            Spark p = _sparks[i];
            if (!p.alive) continue;
            p.age += dt;
            float k = p.age / p.life;
            if (k >= 1f || p.tr == null) { p.alive = false; if (p.tr != null) p.tr.gameObject.SetActive(false); _sparks[i] = p; continue; }
            p.vel += Vector3.down * 9f * dt;
            p.vel *= 1f - dt * 2.5f;
            p.tr.position += p.vel * dt;
            Color c = Color.Lerp(Color.white, new Color(0.45f, 0.9f, 0.55f), k * 1.4f);
            c.a = (k > 0.6f && (Time.frameCount & 1) == 1) ? 0.3f : 1f;
            p.sr.color = c;
            _sparks[i] = p;
        }
    }

    void LateUpdate()
    {
        if (_volume == null) return;
        float w = _envelope;
        if (_flicker && (Time.frameCount & 1) == 1) w *= 0.35f;
        _volume.weight = w;
    }

    void BuildVolume()
    {
        var go = new GameObject("HitFeedbackVolume");
        go.transform.SetParent(transform, false);
        foreach (Volume v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            if (v.isGlobal) { go.layer = v.gameObject.layer; break; }   // 카메라 Volume Mask 에 걸리도록 기존과 같은 레이어

        _volume = go.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = volumePriority;
        _volume.weight = 0f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "HitFeedback (runtime)";
        _color = profile.Add<ColorAdjustments>();
        _chroma = profile.Add<ChromaticAberration>();
        _bloom = profile.Add<Bloom>();
        _lens = profile.Add<LensDistortion>();
        _vignette = profile.Add<Vignette>();
        _volume.sharedProfile = profile;
    }

    // ------------------------------------------------------------------ API

    /// <summary>카드가 젖혀지기 시작할 때. 화면이 살짝 어두워졌다가 Impact 에서 터짐</summary>
    public void Anticipate()
    {
        if (!useAnticipation) return;
        Play(anticipation, 1f, anticipationHold);
    }

    /// <summary>맞는 순간. amount = 실제 피해/회복/방어/상태 수치</summary>
    public void Impact(HitKind kind, int amount)
    {
        float a = Mathf.Clamp01(Mathf.Abs(amount) / (float)Mathf.Max(1, bigHit));
        float k = kind switch
        {
            HitKind.Damage => Mathf.Lerp(0.4f, 1f, a),
            HitKind.Block => Mathf.Lerp(0.5f, 0.85f, a),
            HitKind.Debuff => 0.6f,
            _ => 0.5f,
        };

        if (useVolumePunch)
        {
            Look look = kind switch
            {
                HitKind.Damage => damage,
                HitKind.Block => block,
                HitKind.Debuff => debuff,
                _ => buff,
            };
            Play(look, k, 0f);
        }

        if (useHitStop && (kind == HitKind.Damage || kind == HitKind.Block))
            StartCoroutine(HitStop(Mathf.Lerp(hitStopMin, hitStopMax, k)));
    }

    /// <summary>카드 -> 대상 빛줄기. travel 동안 날아가 도착 직전 가속 (CardExecute 의 돌진 시간과 맞춤)</summary>
    /// <param name="thickness">두께 배율 (차징 공격에서 굵게)</param>
    /// <param name="sparks">스파크 수: 날아가는 동안 머리에서, 머무는 동안 광선을 따라 (도착할 때 1/3 만큼 더 터짐)</param>
    /// <param name="linger">도착한 뒤 광선이 떨리며 머무는 시간 (0 이면 바로 사라짐)</param>
    /// <param name="impact">착탄 지점에 터지는 원형 빛 지름 (0 이면 없음)</param>
    public void Beam(Vector3 from, Vector3 to, float travel, bool positive, float thickness = 1f, int sparks = 0, float linger = 0f, float impact = 0f)
    {
        if (!useBeam || travel <= 0f) return;

        var go = new GameObject("HitBeam");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.numCapVertices = 0;
        lr.alignment = LineAlignment.View;
        lr.sortingOrder = beamSortingOrder;
        lr.sharedMaterial = BeamMaterial();
        lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.7f, 0.6f), new Keyframe(1f, 1f));
        lr.widthMultiplier = beamWidth * thickness * (positive ? 1.3f : 1f);
        lr.startColor = new Color(1f, 1f, 1f, 0f);
        lr.endColor = Color.white;

        // 카메라 쪽으로 살짝 당겨 다른 오브젝트에 묻히지 않게
        Camera cam = Camera.main;
        Vector3 toCam = cam != null ? -cam.transform.forward * 0.5f : Vector3.zero;
        from += toCam; to += toCam;
        lr.SetPosition(0, from);
        lr.SetPosition(1, from);

        // 1) 날아감 (머리는 가속, 꼬리는 일정 거리 뒤)  2) 꼬리가 머리로 빨려 들어가며 사라짐
        Vector3 path = to - from;
        Vector3 side = Vector3.Cross(path.normalized, Vector3.forward);
        int emitted = 0;
        Sequence seq = DOTween.Sequence().SetLink(go);
        seq.Append(DOVirtual.Float(0f, 1f, travel, u =>
        {
            float head = u * u, tail = Mathf.Max(0f, head - beamTail);
            Vector3 headPos = Vector3.Lerp(from, to, head);
            lr.SetPosition(0, Vector3.Lerp(from, to, tail));
            lr.SetPosition(1, headPos);
            // 머리에서 양옆으로 스파크
            int want = Mathf.RoundToInt(sparks * u);
            for (; emitted < want; emitted++)
            {
                Vector3 v = side * UnityEngine.Random.Range(-1f, 1f) * UnityEngine.Random.Range(2f, 6f) - path.normalized * UnityEngine.Random.Range(0.5f, 3f) + Vector3.up * UnityEngine.Random.Range(0f, 3f);
                EmitSpark(headPos, v, UnityEngine.Random.Range(0.2f, 0.45f), UnityEngine.Random.value < 0.3f ? 0.07f : 0.045f);
            }
        }).SetEase(Ease.Linear));
        seq.AppendCallback(() =>
        {
            if (sparks > 0) SparkBurst(to, sparks / 3, 6f);
            if (impact > 0f) ImpactGlow(to, impact);
        });
        if (linger > 0f)
        {
            // 도착한 광선이 시작점까지 이어진 채 두께가 떨리며 머묾 + 광선을 따라 스파크
            float baseWidth = lr.widthMultiplier;
            int lingerSparks = sparks / 2, lingerEmitted = 0;
            seq.Append(DOVirtual.Float(0f, 1f, linger, u =>
            {
                lr.SetPosition(0, from);
                lr.SetPosition(1, to);
                float flick = 0.8f + 0.2f * Mathf.Sin(u * linger * 70f) + 0.1f * Mathf.Sin(u * linger * 157f);
                lr.widthMultiplier = baseWidth * flick * (1f - 0.35f * u);
                int want = Mathf.RoundToInt(lingerSparks * u);
                for (; lingerEmitted < want; lingerEmitted++)
                {
                    Vector3 at = Vector3.Lerp(from, to, UnityEngine.Random.Range(0.1f, 1f));
                    Vector3 v = side * UnityEngine.Random.Range(-1f, 1f) * UnityEngine.Random.Range(1.5f, 4.5f) + Vector3.up * UnityEngine.Random.Range(0f, 2f);
                    EmitSpark(at, v, UnityEngine.Random.Range(0.15f, 0.35f), 0.045f);
                }
            }).SetEase(Ease.Linear));
            // 시작점 쪽부터 빨려 들어가며 가늘어져 사라짐
            seq.Append(DOVirtual.Float(0f, 1f, 0.12f, u =>
            {
                lr.SetPosition(0, Vector3.Lerp(from, to, u));
                lr.SetPosition(1, to);
                lr.widthMultiplier = baseWidth * 0.65f * (1f - u);
            }).SetEase(Ease.InQuad));
        }
        else
        {
            seq.Append(DOVirtual.Float(1f - beamTail, 1f, 0.06f, tail =>
            {
                lr.SetPosition(0, Vector3.Lerp(from, to, tail));
                lr.SetPosition(1, to);
            }));
        }
        seq.OnComplete(() => { if (go != null) Destroy(go); });
    }

    // ------------------------------------------------------------------ charge

    /// <summary>밸류로 차징 세기 계산. 기준 밸류 미만이면 active = false</summary>
    public Charge GetCharge(int value)
    {
        if (!useCharge || value < chargeThreshold) return default;
        float k = Mathf.Clamp01((value - chargeThreshold) / (float)Mathf.Max(1, chargeMaxValue - chargeThreshold));
        return new Charge
        {
            active = true, k = k,
            time = L(chargeTime, k), tilt = L(chargeTilt, k), back = L(chargeBack, k),
            shake = L(chargeShake, k), shakeHz = L(chargeShakeHz, k), glow = L(glowSize, k),
            thickness = L(beamThickness, k), linger = L(beamLinger, k), impact = L(impactGlow, k),
            sparks = Mathf.RoundToInt(L(beamSparks, k)),
        };
    }

    static float L(Vector2 range, float k) => Mathf.Lerp(range.x, range.y, k);

    /// <summary>
    /// 차징 시전 전체: 카드(DOTweenManager.CardExecuteCharged) + 화면 조임 + 원형 빛 + 발사 순간 굵은 빛줄기/스파크/착탄 빛.
    /// onHit 는 타격 순간 (피해 적용, 흔들림은 기존처럼 ModifyHealth 쪽에서)
    /// </summary>
    public Sequence PlayChargedCast(Transform card, Vector3 forwardDir, Transform target, Charge c, Action onHit)
    {
        // 화면: 차징 동안 점점 조여듦 (예고 연출을 차징 시간만큼 길게)
        if (useAnticipation)
        {
            Look look = anticipation;
            look.attack = c.time * 0.85f;
            Play(look, 1f, c.time * 0.15f + 0.2f);
        }

        // 카드 위 원형 빛: 서서히 커짐 (u^0.7: 처음부터 보이게 커지다가 끝에서 완만하게)
        Transform glow = card != null ? SpawnGlow() : null;
        Vector3 toCam = Camera.main != null ? -Camera.main.transform.forward * 0.3f : Vector3.zero;
        Tween grow = null;
        if (glow != null)
            grow = DOVirtual.Float(0f, 1f, c.time, u =>
            {
                if (glow == null || card == null) return;
                float flick = 1f + 0.08f * Mathf.Sin(Time.time * 45f) + 0.04f * Mathf.Sin(Time.time * 97f);
                glow.position = card.position + toCam;
                glow.localScale = Vector3.one * (c.glow * Mathf.Pow(u, 0.7f) * flick);
            }).SetEase(Ease.Linear);

        return DOTweenManager.CardExecuteCharged(card, forwardDir, c.time, c.back, c.tilt, c.shake, c.shakeHz,
            onHitMoment: onHit,
            onLaunch: dash =>
            {
                grow?.Kill();
                ReleaseGlow(glow);
                if (card != null && target != null) Beam(card.position, target.position, dash, false, c.thickness, c.sparks, c.linger, c.impact);
            },
            onFinish: () =>
            {
                grow?.Kill();
                if (glow != null) Destroy(glow.gameObject);
            });
    }

    /// <summary>pos 에서 사방으로 스파크 (위쪽으로 조금 더)</summary>
    public void SparkBurst(Vector3 pos, int count, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f), sp = UnityEngine.Random.Range(speed * 0.4f, speed);
            Vector3 v = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.8f + 0.4f, 0f) * sp;
            EmitSpark(pos, v, UnityEngine.Random.Range(0.25f, 0.55f), UnityEngine.Random.value < 0.3f ? 0.07f : 0.045f);
        }
    }

    void EmitSpark(Vector3 pos, Vector3 vel, float life, float size)
    {
        int slot = -1;
        for (int i = 0; i < _sparks.Count; i++) if (!_sparks[i].alive || _sparks[i].tr == null) { slot = i; break; }
        if (slot < 0)
        {
            if (_sparks.Count >= MaxSparks) return;
            var go = new GameObject("Spark");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelSprite();
            sr.sharedMaterial = BeamMaterial();
            sr.sortingOrder = beamSortingOrder + 1;
            _sparks.Add(new Spark { tr = go.transform, sr = sr });
            slot = _sparks.Count - 1;
        }
        Spark p = _sparks[slot];
        if (p.tr == null)
        {
            // 풀 오브젝트가 지워졌으면 다시 만듦
            _sparks.RemoveAt(slot);
            EmitSpark(pos, vel, life, size);
            return;
        }
        p.alive = true; p.age = 0f; p.life = life; p.vel = vel;
        p.tr.position = pos;
        p.tr.localScale = Vector3.one * size;
        p.tr.gameObject.SetActive(true);
        _sparks[slot] = p;
    }

    Transform SpawnGlow()
    {
        var go = new GameObject("ChargeGlow");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GlowSprite();
        if (_glowMaterial == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            _glowMaterial = new Material(sh) { name = "ChargeGlow (runtime)", color = glowColor };   // HDR 이라 블룸을 받음
        }
        sr.sharedMaterial = _glowMaterial;
        sr.sortingOrder = glowSortingOrder;
        go.transform.localScale = Vector3.zero;
        return go.transform;
    }

    void ReleaseGlow(Transform glow)
    {
        // 발사 순간: 빛이 한 번 부풀었다가 0.08초 만에 꺼짐
        if (glow == null) return;
        Vector3 s = glow.localScale;
        DOTween.Sequence().SetLink(glow.gameObject)
            .Append(glow.DOScale(s * 1.35f, 0.03f))
            .Append(glow.DOScale(Vector3.zero, 0.08f).SetEase(Ease.InQuad))
            .OnComplete(() => { if (glow != null) Destroy(glow.gameObject); });
    }

    /// <summary>착탄 지점에만 터지는 원형 빛 (화면 전체가 아니라 맞은 자리만 밝아짐)</summary>
    void ImpactGlow(Vector3 pos, float size)
    {
        Transform g = SpawnGlow();
        var sr = g.GetComponent<SpriteRenderer>();
        Camera cam = Camera.main;
        g.position = pos + (cam != null ? -cam.transform.forward * 0.3f : Vector3.zero);
        DOTween.Sequence().SetLink(g.gameObject)
            .Append(g.DOScale(Vector3.one * size, 0.06f).SetEase(Ease.OutQuad))
            .Append(g.DOScale(Vector3.one * size * 0.6f, 0.3f).SetEase(Ease.InQuad))
            .Join(sr.DOFade(0f, 0.3f).SetEase(Ease.InQuad))
            .OnComplete(() => { if (g != null) Destroy(g.gameObject); });
    }

    Sprite PixelSprite()
    {
        if (_pixelSprite != null) return _pixelSprite;
        var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);   // 1 유닛 사각형
        return _pixelSprite;
    }

    /// <summary>픽셀 원형 빛: 중심이 밝고 바깥으로 디더링되며 흐려짐 (CRT 픽셀 느낌), 1 유닛 지름</summary>
    Sprite GlowSprite()
    {
        if (_glowSprite != null) return _glowSprite;
        const int N = 48;
        float[] bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                float v = Mathf.Clamp01(1f - r);
                v = v * v * (0.6f + 0.4f * v);                                   // 중심에 몰린 빛
                float threshold = (bayer[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;
                float a = Mathf.Floor(v * 4f + threshold) / 4f;                  // 4단계 디더링
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
        tex.Apply();
        _glowSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        return _glowSprite;
    }

    // ------------------------------------------------------------------ internals

    void Play(Look look, float peak, float hold)
    {
        Apply(look);
        _flicker = false;
        _envTween?.Kill();
        _envTween = DOTween.Sequence().SetUpdate(true);   // 히트스톱 중에도 진행
        if (look.attack > 0f) _envTween.Append(DOTween.To(() => _envelope, x => _envelope = x, peak, look.attack).SetEase(Ease.OutQuad));
        else _envTween.AppendCallback(() => _envelope = peak);
        if (hold > 0f) _envTween.AppendInterval(hold);
        _envTween.AppendCallback(() => _flicker = look.flicker);
        _envTween.Append(DOTween.To(() => _envelope, x => _envelope = x, 0f, Mathf.Max(0.01f, look.release)).SetEase(Ease.OutQuad));
        _envTween.OnComplete(() => { _envelope = 0f; _flicker = false; });
    }

    void Apply(Look look)
    {
        _color.postExposure.Override(look.exposure);
        Set(_chroma.intensity, look.chroma, 0f, 1f);
        if (look.bloom > 0f) _bloom.intensity.Override(look.bloom); else _bloom.intensity.overrideState = false;
        Set(_lens.intensity, look.lens, -1f, 1f);
        Set(_vignette.intensity, look.vignette, 0f, 1f);
    }

    static void Set(ClampedFloatParameter p, float v, float min, float max)
    {
        if (Mathf.Approximately(v, 0f)) { p.overrideState = false; return; }
        p.Override(Mathf.Clamp(v, min, max));
    }

    IEnumerator HitStop(float seconds)
    {
        if (_stops++ == 0) { _savedTimeScale = Time.timeScale; Time.timeScale = hitStopTimeScale; }
        yield return new WaitForSecondsRealtime(seconds);
        if (--_stops == 0) Time.timeScale = _savedTimeScale;
    }

    Material BeamMaterial()
    {
        if (beamMaterial != null) return beamMaterial;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        beamMaterial = new Material(sh) { name = "HitBeam (runtime)" };
        beamMaterial.color = beamColor;   // HDR 이라 블룸을 받음
        return beamMaterial;
    }
}
