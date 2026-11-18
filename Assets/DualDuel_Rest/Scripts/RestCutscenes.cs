using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 휴식 씬 컷씬 3종. 모두 timeScale 과 무관하게(unscaled) 돌아간다.
///
/// 회복 (6초)    0~2   기계팔 3개가 코어를 두 번 용접 (지직... 파바박!) 스파크 + 노출 튐
///               2~3   흰 화면 (이 순간 회복 적용, 균열 사라짐)
///               3~6   흰 화면이 easeOutSine 으로 걷힘 -> 글리치 씬 전환
/// 강화 (6.9초)  0~3   카드가 가운데로, 왜곡 + 줌인 easeInCubic, 마지막에 쑤욱 빨려듦
///                     2~3 왜곡이 끝나기 1초 전부터 흰 화면이 차올라, 최대 왜곡 순간엔 이미 덮여 있음
///               3~3.5 번쩍 (강화 적용, 카드 교체)       3.5~4.5 흰 화면 easeOutSine 으로 걷힘
///               4.5~5.5 필름 같은 빛줄기가 대각선으로 훑음 (easeInOutExpo)
///               5.5~6.1 Z 회전 +8 -8 +8 -8 0 딸랑딸랑    6.1~6.9 대기 -> 글리치 씬 전환
/// 제거 (8.5초)  0~3   왜곡 + 줌인 easeInCubic, 카드가 x ±0.1 로 초당 3번 덜덜
///               2~3   왜곡이 계속 커지는 동안 화면과 카드가 easeInCubic 으로 하얘지며 더 크게, 더 빠르게 덜덜
///               3     최대 왜곡 순간 펑 (제거 적용)   3~4.5 흰 화면 유지   4.5~6.5 흰 화면 easeOutSine 으로 걷힘
///               3~8.5 재처럼 흰 가루가 흩날려 사라짐 -> 글리치 씬 전환
/// </summary>
public class RestCutscenes : MonoBehaviour
{
    [Header("연결 (RestSceneSetup 툴이 자동으로 채움)")]
    public RestSceneBridge bridge;
    public CanvasGroup group;               // 컷씬 캔버스 (모든 UI 위, 스캔라인 아래)
    public Image white;                     // 화면 전체 흰색
    public Image focusDim;                  // 카드 연출 때 뒤를 어둡게
    public RectTransform focusRoot;         // 카드가 올라오는 자리 (화면 중앙)
    public RectTransform[] zoomTargets;     // 줌인 때 함께 커지는 것들 (배경, UI 루트)
    public GameObject weldStage;
    public GameObject cracks;
    public RobotArm[] arms;
    public RectTransform particleLayer;
    public Image burst;
    public Sprite shineSprite;

    [Header("회복: 기계팔 목표 (weldStage 기준 좌표)")]
    public Vector2[] idleTargets = { new Vector2(-420f, -60f), new Vector2(0f, 220f), new Vector2(420f, -60f) };
    public Vector2[] weldTargets = { new Vector2(-110f, -80f), new Vector2(10f, 50f), new Vector2(110f, -120f) };

    [Header("카드 연출")]
    [Tooltip("가운데로 온 카드의 크기 배율")] public float focusScale = 1.5f;
    [Tooltip("최대 왜곡 (Lens Distortion, 음수 = 빨려듦)")] public float maxLens = -0.9f;
    [Tooltip("줌인 최대 배율 (zoomTargets)")] public float maxZoom = 1.35f;
    [Tooltip("제거 연출의 덜덜 폭 (월드 단위)")] public float shakeAmplitude = 0.1f;
    [Tooltip("왜곡이 끝나기 몇 초 전부터 흰 화면이 차오를지 (강화/제거). 최대 왜곡 순간엔 이미 덮여 있어 멈춘 왜곡이 보이지 않음")]
    public float whiteLead = 1f;

    [Header("사운드 (선택)")]
    public AudioClip weldSfx;
    public AudioClip flashSfx;
    public AudioClip shineSfx;
    public AudioClip explodeSfx;

    public bool IsPlaying { get; private set; }

    // ------------------------------------------------------------------ runtime state

    Volume _volume;
    LensDistortion _lens;
    ColorAdjustments _color;
    Bloom _bloom;
    float _spike;            // 순간 노출 튐 (스파크), 매 프레임 감쇠
    AudioSource _audio;

    struct Particle { public RectTransform rt; public Image img; public Vector2 pos, vel; public float age, life; public bool ash, alive; public float seed; }
    readonly List<Particle> _particles = new List<Particle>();
    const int PoolSize = 360;

    // ------------------------------------------------------------------ setup

    void Awake()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        BuildVolume();
        BuildPool();
        if (group != null) group.gameObject.SetActive(false);
    }

    void BuildVolume()
    {
        var go = new GameObject("RestCutsceneVolume");
        go.transform.SetParent(transform, false);
        foreach (Volume v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            if (v.isGlobal) { go.layer = v.gameObject.layer; break; }
        _volume = go.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 120;
        _volume.weight = 0f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _lens = profile.Add<LensDistortion>();
        _color = profile.Add<ColorAdjustments>();
        _bloom = profile.Add<Bloom>();
        _volume.sharedProfile = profile;
    }

    void BuildPool()
    {
        if (particleLayer == null) return;
        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject("p", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(particleLayer, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            go.SetActive(false);
            _particles.Add(new Particle { rt = rt, img = img });
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _spike = Mathf.MoveTowards(_spike, 0f, dt * 6f);
        if (_volume != null && _volume.weight > 0f)
        {
            _color.postExposure.Override(_baseExposure + _spike);
            _bloom.intensity.Override(0.7f + _spike * 3f);
        }
        UpdateParticles(dt);
    }

    float _baseExposure = 0.2f;

    // ------------------------------------------------------------------ 1. heal

    public IEnumerator PlayHeal()
    {
        Begin();
        weldStage.SetActive(true);
        if (cracks != null) cracks.SetActive(true);
        for (int i = 0; i < arms.Length; i++) { arms[i].target = idleTargets[i]; arms[i].Solve(); }
        group.alpha = 0f;
        yield return Run(0.15f, u => group.alpha = u);

        // 지직... (작은 버스트 5번)   파바박! (큰 버스트 9번)
        float[] bursts = { 0.40f, 0.47f, 0.56f, 0.70f, 0.79f, 1.25f, 1.31f, 1.36f, 1.42f, 1.50f, 1.56f, 1.62f, 1.70f, 1.79f };
        int next = 0;
        float t0 = Time.unscaledTime;
        while (true)
        {
            float t = Time.unscaledTime - t0;
            if (t >= 2f) break;
            float reach = WeldReach(t);
            bool welding = (t > 0.35f && t < 0.88f) || (t > 1.2f && t < 1.9f);
            for (int i = 0; i < arms.Length; i++)
            {
                Vector2 jitter = welding ? new Vector2(Mathf.Sin(t * 53f + i * 7f), Mathf.Sin(t * 41f + i * 3f)) * 4f : Vector2.zero;
                arms[i].target = Vector2.Lerp(idleTargets[i], weldTargets[i], reach) + jitter;
            }
            while (next < bursts.Length && t >= bursts[next])
            {
                bool big = bursts[next] > 1.2f;
                foreach (RobotArm a in arms) { a.Solve(); SpawnSparks(a.Tip, big ? 18 : 9, big); }
                _spike = Mathf.Max(_spike, big ? 1.1f : 0.6f);
                Flicker(big ? 0.25f : 0.12f);
                if (weldSfx != null) _audio.PlayOneShot(weldSfx, big ? 1f : 0.6f);
                next++;
            }
            yield return null;
        }

        // 번쩍: 흰 화면 1초 유지 (그 사이 회복 적용, 균열 제거, 팔 접기)
        SetWhite(1f);
        _spike = 1.5f;
        if (flashSfx != null) _audio.PlayOneShot(flashSfx);
        if (bridge != null) bridge.ApplyHeal(bridge.HealAmount);
        if (cracks != null) cracks.SetActive(false);
        for (int i = 0; i < arms.Length; i++) arms[i].target = idleTargets[i];
        yield return Wait(1f);
        yield return Run(3f, u => SetWhite(1f - EaseOutSine(u)));
        Leave();
    }

    static float WeldReach(float t)
    {
        if (t < 0.1f) return 0f;
        if (t < 0.35f) return EaseOutCubic((t - 0.1f) / 0.25f);
        if (t < 0.88f) return 1f;
        if (t < 1.05f) return Mathf.Lerp(1f, 0.55f, EaseOutCubic((t - 0.88f) / 0.17f));
        if (t < 1.2f) return Mathf.Lerp(0.55f, 1f, EaseOutCubic((t - 1.05f) / 0.15f));
        return 1f;
    }

    // ------------------------------------------------------------------ 2. upgrade

    public IEnumerator PlayUpgrade(RectTransform cardView, object card)
    {
        Begin();
        cardView.SetParent(focusRoot, true);
        LockCard(cardView);
        Vector2 from = cardView.anchoredPosition;
        float s0 = cardView.localScale.x;

        // 0~3초: 가운데로 오며 왜곡 + 줌인, 마지막 10% 에 쑤욱 빨려듦
        yield return Run(3f, u =>
        {
            float e = EaseInCubic(u);
            focusDim.color = new Color(0f, 0f, 0f, 0.6f * Mathf.Clamp01(u * 4f));
            cardView.anchoredPosition = Vector2.Lerp(from, Vector2.zero, EaseOutCubic(Mathf.Clamp01(u * 6f)));
            float grow = Mathf.Lerp(s0, focusScale * 1.25f, e);
            float suck = u < 0.9f ? 1f : Mathf.Lerp(1f, 0.12f, EaseInCubic((u - 0.9f) / 0.1f));
            cardView.localScale = Vector3.one * grow * suck;
            Zoom(1f + (maxZoom - 1f) * e);
            Lens(maxLens * e);
            SetWhite(EaseInCubic(LeadIn(u, 3f)));   // 마지막 whiteLead 초 동안 0 -> 1
        });

        // 번쩍 0.5초: 강화 적용, 카드 교체
        SetWhite(1f);
        _spike = 1.5f;
        if (flashSfx != null) _audio.PlayOneShot(flashSfx);
        Lens(0f); Zoom(1f);
        if (bridge != null) bridge.ApplyUpgrade(card);
        // 같은 카드 UI 에 강화된 값을 다시 그림 (UIViewerCard.RefreshValues). 안 되면 새로 만듦
        RectTransform view = cardView;
        if (bridge == null || !bridge.RefreshCardView(view, card))
        {
            Destroy(cardView.gameObject);
            view = bridge != null ? bridge.CreateCardView(card, focusRoot, false) : null;
            if (view != null) LockCard(view);
        }
        if (view != null)
        {
            view.anchoredPosition = Vector2.zero;
            view.localScale = Vector3.one * focusScale;
            view.localEulerAngles = Vector3.zero;
        }
        yield return Wait(0.5f);
        yield return Run(1f, u => SetWhite(1f - EaseOutSine(u)));

        // 필름처럼 빛이 훑고 지나감
        if (view != null && shineSprite != null)
        {
            RectTransform shine = MakeShine(view);
            float w = view.rect.width;
            if (shineSfx != null) _audio.PlayOneShot(shineSfx);
            yield return Run(1f, u => shine.anchoredPosition = new Vector2(Mathf.Lerp(-w, w, EaseInOutExpo(u)), 0f));
            Destroy(shine.parent.gameObject);
        }
        else yield return Wait(1f);

        // 딸랑딸랑: Z 회전 +8 -8 +8 -8 0
        float[] keys = { 0f, 8f, -8f, 8f, -8f, 0f };
        if (view != null)
            yield return Run(0.6f, u =>
            {
                float k = u * (keys.Length - 1);
                int i = Mathf.Min(keys.Length - 2, Mathf.FloorToInt(k));
                view.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(keys[i], keys[i + 1], EaseInOutSine(k - i)));
            });
        yield return Wait(0.8f);
        Leave();
    }

    // ------------------------------------------------------------------ 3. remove

    public IEnumerator PlayRemove(RectTransform cardView, object card)
    {
        Begin();
        cardView.SetParent(focusRoot, true);
        LockCard(cardView);
        Vector2 from = cardView.anchoredPosition;
        float s0 = cardView.localScale.x;
        Image cardWhite = MakeCardWhite(cardView);
        Vector3 basePos = Vector3.zero;

        // 0~3초: 왜곡 + 줌인이 끝까지 계속 커짐. 마지막 whiteLead 초 동안 화면과 카드가 하얘지며
        //        덜덜이 더 크고 빨라짐 -> 최대 왜곡 순간엔 흰 화면이 덮고 있어 멈춘 왜곡이 보이지 않음
        float phase = 0f, prevT = 0f;
        yield return Run(3f, (u, t) =>
        {
            float e = EaseInCubic(u);
            float w = EaseInCubic(LeadIn(u, 3f));
            focusDim.color = new Color(0f, 0f, 0f, 0.6f * Mathf.Clamp01(u * 4f));
            cardView.anchoredPosition = Vector2.Lerp(from, Vector2.zero, EaseOutCubic(Mathf.Clamp01(u * 6f)));
            basePos = cardView.position;
            // 진동수가 바뀌어도 튀지 않도록 위상을 누적
            float hz = 3f + 4f * w;
            phase += (t - prevT) * hz * Mathf.PI * 2f;
            prevT = t;
            cardView.position = basePos + Vector3.right * (shakeAmplitude * (1f + 1.5f * w) * Mathf.Sin(phase));
            cardView.localScale = Vector3.one * Mathf.Lerp(s0, focusScale, e);
            cardWhite.color = new Color(1f, 1f, 1f, w);
            Zoom(1f + (maxZoom - 1f) * e);
            Lens(maxLens * e);
            SetWhite(0.85f * w);
        });

        // 펑: 제거 적용, 화면을 채우는 폭발, 흰 화면 1.5초
        if (explodeSfx != null) _audio.PlayOneShot(explodeSfx);
        if (bridge != null) bridge.RemoveCard(card);
        Vector2 ashOrigin = Vector2.zero;
        Vector2 ashSize = cardView.rect.size * cardView.localScale.x;
        cardView.gameObject.SetActive(false);
        StartCoroutine(Burst());
        _spike = 1.6f;
        Lens(0f); Zoom(1f);
        focusDim.color = Color.clear;
        StartCoroutine(EmitAsh(ashOrigin, ashSize, 3.5f));
        yield return Run(0.12f, u => SetWhite(Mathf.Lerp(0.85f, 1f, u)));
        yield return Wait(1.5f - 0.12f);
        yield return Run(2f, u => SetWhite(1f - EaseOutSine(u)));
        // 재가 다 날아갈 때까지 (약 2초)
        while (AnyAlive()) yield return null;
        Destroy(cardView.gameObject);
        Leave();
    }

    IEnumerator Burst()
    {
        if (burst == null) yield break;
        burst.gameObject.SetActive(true);
        var rt = burst.rectTransform;
        yield return Run(0.3f, u =>
        {
            rt.localScale = Vector3.one * Mathf.Lerp(0.3f, 10f, EaseOutCubic(u));
            burst.color = new Color(1f, 1f, 1f, 1f - u * 0.3f);
        });
        burst.gameObject.SetActive(false);
    }

    IEnumerator EmitAsh(Vector2 center, Vector2 size, float duration)
    {
        float acc = 0f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            acc += Time.unscaledDeltaTime * Mathf.Lerp(140f, 40f, t / duration);   // 처음에 많이, 점점 적게
            while (acc >= 1f)
            {
                acc -= 1f;
                Vector2 p = center + new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f) * size.x, UnityEngine.Random.Range(-0.5f, 0.5f) * size.y);
                Vector2 v = new Vector2(UnityEngine.Random.Range(-30f, 90f), UnityEngine.Random.Range(30f, 110f));
                Spawn(p, v, UnityEngine.Random.Range(1.2f, 2f), UnityEngine.Random.value < 0.3f ? 6f : 4f, true);
            }
            yield return null;
        }
    }

    // ------------------------------------------------------------------ particles

    void SpawnSparks(Vector2 at, int n, bool big)
    {
        for (int i = 0; i < n; i++)
        {
            float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f), sp = UnityEngine.Random.Range(big ? 300f : 200f, big ? 900f : 600f);
            Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.7f + 0.45f) * sp;
            Spawn(at, v, UnityEngine.Random.Range(0.18f, big ? 0.55f : 0.4f), UnityEngine.Random.value < 0.3f ? 6f : 4f, false);
        }
    }

    void Spawn(Vector2 pos, Vector2 vel, float life, float size, bool ash)
    {
        for (int i = 0; i < _particles.Count; i++)
        {
            Particle p = _particles[i];
            if (p.alive) continue;
            p.alive = true; p.ash = ash; p.pos = pos; p.vel = vel; p.age = 0f; p.life = life; p.seed = UnityEngine.Random.value * 10f;
            p.rt.sizeDelta = new Vector2(size, size);
            p.rt.gameObject.SetActive(true);
            _particles[i] = p;
            return;
        }
    }

    void UpdateParticles(float dt)
    {
        for (int i = 0; i < _particles.Count; i++)
        {
            Particle p = _particles[i];
            if (!p.alive) continue;
            p.age += dt;
            float k = p.age / p.life;
            if (k >= 1f) { p.alive = false; p.rt.gameObject.SetActive(false); _particles[i] = p; continue; }
            if (p.ash)
            {
                // 재: 위로 떠오르며 바람에 흔들리고, 천천히 느려짐
                p.vel += new Vector2(Mathf.Sin(p.age * 3f + p.seed) * 60f, 10f) * dt;
                p.vel *= 1f - dt * 0.4f;
                float a = k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f;
                bool flick = ((int)((p.age + p.seed) * 12f) & 3) == 0;
                p.img.color = new Color(0.92f, 1f, 0.94f, a * (flick ? 0.5f : 1f));
            }
            else
            {
                // 스파크: 중력, 흰색에서 초록으로 식으며 깜빡임
                p.vel += Vector2.down * 1400f * dt;
                p.vel *= 1f - dt * 1.5f;
                Color hot = Color.white, cool = new Color(0.45f, 0.9f, 0.55f);
                Color c = Color.Lerp(hot, cool, k * 1.6f);
                c.a = (k > 0.6f && ((int)(p.age * 30f) & 1) == 1) ? 0.3f : 1f;
                p.img.color = c;
            }
            p.pos += p.vel * dt;
            // 픽셀 스냅 (캔버스 2단위 = 원본 1px)
            p.rt.anchoredPosition = new Vector2(Mathf.Round(p.pos.x * 0.5f) * 2f, Mathf.Round(p.pos.y * 0.5f) * 2f);
            _particles[i] = p;
        }
    }

    bool AnyAlive()
    {
        foreach (Particle p in _particles) if (p.alive) return true;
        return false;
    }

    // ------------------------------------------------------------------ helpers

    void Begin()
    {
        IsPlaying = true;
        group.gameObject.SetActive(true);
        group.alpha = 1f;
        group.blocksRaycasts = true;
        SetWhite(0f);
        focusDim.color = Color.clear;
        if (weldStage != null) weldStage.SetActive(false);
        if (burst != null) burst.gameObject.SetActive(false);
        _volume.weight = 1f;
        Lens(0f);
        _bloom.threshold.overrideState = false;
    }

    void Leave()
    {
        // 컷씬은 끄지 않은 채로 글리치 전환 (지금 화면이 찢어지며 넘어감)
        if (bridge != null) bridge.LeaveRest();
        else Debug.Log("[RestCutscenes] 브리지가 없어 씬 전환을 건너뜀");
    }

    void SetWhite(float a) => white.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a));

    /// <summary>
    /// 연출 중인 카드가 마우스를 받지 않게 함. UIViewerCard 프리팹의 TooltipTrigger 가
    /// 컷씬 위(툴팁 캔버스 300 > 컷씬 180)에 툴팁을 띄우지 않도록, 이미 떠 있는 툴팁도 닫음
    /// </summary>
    static void LockCard(RectTransform card)
    {
        var group = card.GetComponent<CanvasGroup>();
        if (group == null) group = card.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        if (TooltipPanel.Instance != null) TooltipPanel.Instance.HideTooltip();
    }

    void Flicker(float a) => StartCoroutine(Run(0.06f, u => { if (white.color.a < 0.99f) SetWhite(a * (1f - u)); }));

    void Lens(float v)
    {
        _lens.intensity.Override(Mathf.Clamp(v, -1f, 1f));
        _lens.center.Override(new Vector2(0.5f, 0.5f));
    }

    void Zoom(float z)
    {
        if (zoomTargets == null) return;
        foreach (RectTransform r in zoomTargets) if (r != null) r.localScale = Vector3.one * z;
    }

    RectTransform MakeShine(RectTransform card)
    {
        var mask = new GameObject("ShineMask", typeof(RectTransform), typeof(RectMask2D));
        var mrt = (RectTransform)mask.transform;
        mrt.SetParent(card, false);
        mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
        var go = new GameObject("Shine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(mrt, false);
        var img = go.GetComponent<Image>();
        img.sprite = shineSprite;
        img.raycastTarget = false;
        rt.sizeDelta = new Vector2(card.rect.width * 0.6f, card.rect.height * 1.2f);
        return rt;
    }

    Image MakeCardWhite(RectTransform card)
    {
        var go = new GameObject("WhiteOut", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(card, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        return img;
    }

    static IEnumerator Wait(float s) { for (float t = 0f; t < s; t += Time.unscaledDeltaTime) yield return null; }

    static IEnumerator Run(float duration, Action<float> f) => Run(duration, (u, t) => f(u));

    static IEnumerator Run(float duration, Action<float, float> f)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime) { f(Mathf.Clamp01(t / duration), t); yield return null; }
        f(1f, duration);
    }

    /// <summary>길이 duration 인 구간의 진행도 u 에서, 마지막 whiteLead 초 동안 0 -> 1</summary>
    float LeadIn(float u, float duration)
    {
        float lead = Mathf.Clamp(whiteLead, 0.01f, duration);
        return Mathf.Clamp01((u * duration - (duration - lead)) / lead);
    }

    static float EaseInCubic(float x) => x * x * x;
    static float EaseOutCubic(float x) { x = 1f - x; return 1f - x * x * x; }
    static float EaseOutSine(float x) => Mathf.Sin(x * Mathf.PI * 0.5f);
    static float EaseInOutSine(float x) => -(Mathf.Cos(Mathf.PI * x) - 1f) * 0.5f;
    static float EaseInOutExpo(float x) =>
        x <= 0f ? 0f : x >= 1f ? 1f : x < 0.5f ? Mathf.Pow(2f, 20f * x - 10f) * 0.5f : (2f - Mathf.Pow(2f, -20f * x + 10f)) * 0.5f;
}
