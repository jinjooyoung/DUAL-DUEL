using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum HitKind { Damage, Block, Buff, Debuff }

/// <summary>
/// DUAL DUEL 타격 연출 보강. 기존 DOTweenManager / BattleVFXPlayer / ShakeByDamage 가 하지 않는 것만 담당.
///   Anticipate()  카드가 젖혀질 때 화면이 숨을 들이쉬듯 살짝 어두워짐 (노출 ↓, 비네트 ↑)
///   Beam()        카드에서 대상 초상화까지 빛줄기 (원인 -> 결과 연결)
///   Impact()      맞는 순간 CRT 볼륨 펀치 (노출 플래시, 색수차, 렌즈 왜곡) + 히트스톱
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
    public void Beam(Vector3 from, Vector3 to, float travel, bool positive)
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
        lr.widthMultiplier = beamWidth * (positive ? 1.3f : 1f);
        lr.startColor = new Color(1f, 1f, 1f, 0f);
        lr.endColor = Color.white;

        // 카메라 쪽으로 살짝 당겨 다른 오브젝트에 묻히지 않게
        Camera cam = Camera.main;
        Vector3 toCam = cam != null ? -cam.transform.forward * 0.5f : Vector3.zero;
        from += toCam; to += toCam;
        lr.SetPosition(0, from);
        lr.SetPosition(1, from);

        // 1) 날아감 (머리는 가속, 꼬리는 일정 거리 뒤)  2) 꼬리가 머리로 빨려 들어가며 사라짐
        Sequence seq = DOTween.Sequence().SetLink(go);
        seq.Append(DOVirtual.Float(0f, 1f, travel, u =>
        {
            float head = u * u, tail = Mathf.Max(0f, head - beamTail);
            lr.SetPosition(0, Vector3.Lerp(from, to, tail));
            lr.SetPosition(1, Vector3.Lerp(from, to, head));
        }).SetEase(Ease.Linear));
        seq.Append(DOVirtual.Float(1f - beamTail, 1f, 0.06f, tail =>
        {
            lr.SetPosition(0, Vector3.Lerp(from, to, tail));
            lr.SetPosition(1, to);
        }));
        seq.OnComplete(() => { if (go != null) Destroy(go); });
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
