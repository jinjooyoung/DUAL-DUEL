using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 글리치 씬 전환. 기존 SceneManager.LoadScene 대신 이걸 부르면 된다.
///   SceneTransition.Load("BattleScene");
///   SceneTransition.Load(2);
///
/// 흐름 (모두 timeScale 무시)
///   1. 현재 화면 캡처 -> 화면이 행 단위로 찢어지며 암전        (outTime)
///   2. 비동기 로드                                              (최소 minBlackTime 동안 암전 유지)
///   3. 새 화면 캡처 -> 찢어진 상태로 들어와 안정                 (inTime)
///
/// 씬에 따로 둘 필요 없음: 처음 호출할 때 스스로 만들어져 씬이 바뀌어도 유지된다.
/// 인스펙터에서 값을 바꾸고 싶으면 첫 씬에 빈 오브젝트 + SceneTransition 을 두면 그 값이 쓰인다.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance
    {
        get
        {
            if (_instance == null) new GameObject("[SceneTransition]").AddComponent<SceneTransition>();
            return _instance;
        }
    }
    static SceneTransition _instance;

    /// <summary>전환 중인지 (중복 호출 방지, 입력 막기 등에 사용)</summary>
    public static bool IsTransitioning => _instance != null && _instance._busy;

    /// <summary>로드 직전 (화면이 완전히 암전된 상태). 저장, DOTween 정리 등</summary>
    public static event Action<string> OnBeforeLoad;
    /// <summary>새 씬 로드 직후 (아직 암전 상태). 새 씬 초기화 등</summary>
    public static event Action<string> OnAfterLoad;

    [Header("타이밍 (초)")]
    [Tooltip("기존 화면이 찢어지며 암전되는 시간")] public float outTime = 0.45f;
    [Tooltip("새 화면이 찢어진 채 들어와 안정되는 시간")] public float inTime = 0.5f;
    [Tooltip("로드가 빨라도 이 시간만큼은 암전 유지 (너무 빠르면 끊겨 보임)")] public float minBlackTime = 0.12f;

    [Header("글리치")]
    [Tooltip("지직거림 갱신 횟수 (초당). 낮을수록 뚝뚝 끊기는 아날로그 느낌")] public float stepsPerSecond = 24f;
    [Tooltip("화면을 몇 줄로 자를지")] public int rows = 120;
    [Tooltip("함께 밀리는 굵은 띠의 줄 수")] public int rowsPerBand = 6;
    [Tooltip("최대 밀림 (화면 폭 대비)")] [Range(0f, 0.5f)] public float maxShift = 0.18f;
    [Tooltip("밀림을 이 픽셀 단위로 끊음 (픽셀아트 느낌)")] public float pixelStep = 4f;
    [Tooltip("노이즈로 바뀌는 줄 비율 (최대 강도일 때)")] [Range(0f, 1f)] public float noiseRows = 0.25f;
    [Tooltip("검게 끊기는 줄 비율 (최대 강도일 때)")] [Range(0f, 1f)] public float maxBlackout = 0.55f;
    [Tooltip("신호 잔상 거리 (화면 폭 대비)")] [Range(0f, 0.05f)] public float ghost = 0.012f;
    [ColorUsage(false)] public Color tint = new Color(0.54f, 1f, 0.65f, 1f);

    [Header("사운드 (선택)")]
    public AudioClip glitchSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("셰이더 (비우면 Resources/GlitchTransition 머티리얼 -> Shader.Find 순으로 찾음)")]
    public Material material;

    const string ShaderName = "DUAL DUEL/Glitch Transition";

    Canvas _canvas;
    RawImage _image;
    Material _mat;
    Texture2D _shot;
    AudioSource _audio;
    bool _busy;

    static readonly int IdProgress = Shader.PropertyToID("_Progress");
    static readonly int IdBlackout = Shader.PropertyToID("_Blackout");
    static readonly int IdFade = Shader.PropertyToID("_Fade");
    static readonly int IdStep = Shader.PropertyToID("_Step");
    static readonly int IdScreenW = Shader.PropertyToID("_ScreenW");

    // ------------------------------------------------------------------ API

    public static void Load(string sceneName) => Instance.Begin(sceneName, -1);
    public static void Load(int buildIndex) => Instance.Begin(null, buildIndex);

    /// <summary>씬 로드 없이 현재 화면에 글리치만 한 번 (테스트/연출용)</summary>
    public static void Pulse() => Instance.StartCoroutine(Instance.PulseRoutine());

    void Begin(string sceneName, int buildIndex)
    {
        if (_busy) { Debug.LogWarning("[SceneTransition] 이미 전환 중입니다."); return; }
        StartCoroutine(Run(sceneName, buildIndex));
    }

    // ------------------------------------------------------------------ setup

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        Build();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
        if (_shot != null) Destroy(_shot);
        if (_mat != null) Destroy(_mat);
    }

    void Build()
    {
        // 화면 맨 위 오버레이 캔버스 (카메라 렌더에 포함되지 않아 캡처에도 안 찍힘)
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32000;
        gameObject.AddComponent<GraphicRaycaster>();   // 전환 중 UI 클릭 막기

        var go = new GameObject("Glitch", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        _image = go.GetComponent<RawImage>();
        _image.raycastTarget = true;

        Material src = material != null ? material : Resources.Load<Material>("GlitchTransition");
        if (src != null) _mat = new Material(src);
        else
        {
            Shader sh = Shader.Find(ShaderName);
            if (sh != null) _mat = new Material(sh);
            else Debug.LogError($"[SceneTransition] '{ShaderName}' 셰이더를 찾지 못했습니다. Resources/GlitchTransition 머티리얼이 있는지 확인하세요.");
        }
        _image.material = _mat;

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.ignoreListenerPause = true;

        _canvas.enabled = false;
    }

    // ------------------------------------------------------------------ flow

    IEnumerator Run(string sceneName, int buildIndex)
    {
        _busy = true;
        string label = sceneName ?? $"#{buildIndex}";

        // 1. 기존 화면 캡처 후 찢으며 암전
        yield return new WaitForEndOfFrame();
        Capture();
        Show(true);
        if (glitchSfx != null) _audio.PlayOneShot(glitchSfx, sfxVolume);

        for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime)
        {
            float u = t / outTime;
            Apply(EaseIn(u), maxBlackout * u * u, Smooth(0.55f, 1f, u));
            yield return null;
        }
        Apply(1f, maxBlackout, 1f);

        // 2. 로드 (암전 유지)
        OnBeforeLoad?.Invoke(label);
        AsyncOperation op = sceneName != null ? SceneManager.LoadSceneAsync(sceneName) : SceneManager.LoadSceneAsync(buildIndex);
        float black = 0f;
        while (op != null && !op.isDone) { Tick(); black += Time.unscaledDeltaTime; yield return null; }
        while (black < minBlackTime) { Tick(); black += Time.unscaledDeltaTime; yield return null; }
        yield return null;   // 새 씬의 Start / 레이아웃이 한 번 돌도록
        OnAfterLoad?.Invoke(label);

        // 3. 새 화면 캡처: 오버레이를 한 프레임 끄고 찍음 (그 한 프레임이 '신호가 잡히는' 번쩍임으로 보임)
        _canvas.enabled = false;
        yield return new WaitForEndOfFrame();
        Capture();
        _canvas.enabled = true;

        for (float t = 0f; t < inTime; t += Time.unscaledDeltaTime)
        {
            float u = t / inTime;
            Apply(1f - EaseOut(u), maxBlackout * (1f - u) * (1f - u), 0.35f * (1f - Smooth(0f, 0.35f, u)));
            yield return null;
        }

        Show(false);
        _busy = false;
    }

    IEnumerator PulseRoutine()
    {
        if (_busy) yield break;
        _busy = true;
        yield return new WaitForEndOfFrame();
        Capture();
        Show(true);
        if (glitchSfx != null) _audio.PlayOneShot(glitchSfx, sfxVolume);
        const float time = 0.35f;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float u = t / time, k = u < 0.3f ? u / 0.3f : 1f - (u - 0.3f) / 0.7f;
            Apply(k, maxBlackout * 0.4f * k, 0f);
            yield return null;
        }
        Show(false);
        _busy = false;
    }

    // ------------------------------------------------------------------ helpers

    void Capture()
    {
        if (_shot != null) Destroy(_shot);
        _shot = ScreenCapture.CaptureScreenshotAsTexture();
        _shot.wrapMode = TextureWrapMode.Clamp;
        _shot.filterMode = FilterMode.Point;
        _image.texture = _shot;
    }

    void Show(bool on)
    {
        _canvas.enabled = on;
        if (!on)
        {
            _image.texture = null;
            if (_shot != null) { Destroy(_shot); _shot = null; }
        }
    }

    void Apply(float progress, float blackout, float fade)
    {
        if (_mat == null) return;
        _mat.SetFloat(IdProgress, progress);
        _mat.SetFloat(IdBlackout, blackout);
        _mat.SetFloat(IdFade, fade);
        _mat.SetFloat("_Rows", rows);
        _mat.SetFloat("_BandSize", Mathf.Max(1, rowsPerBand));
        _mat.SetFloat("_MaxShift", maxShift);
        _mat.SetFloat("_PixelStep", Mathf.Max(1f, pixelStep));
        _mat.SetFloat("_NoiseRows", noiseRows);
        _mat.SetFloat("_Ghost", ghost);
        _mat.SetColor("_Tint", tint);
        Tick();
    }

    void Tick()
    {
        if (_mat == null) return;
        _mat.SetFloat(IdStep, Mathf.Floor(Time.unscaledTime * stepsPerSecond));
        _mat.SetFloat(IdScreenW, Screen.width);
    }

    static float EaseIn(float x) { x = Mathf.Clamp01(x); return x * x; }
    static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x); }
    static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));
}
