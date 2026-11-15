using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DUAL DUEL CRT 카드 이펙트 재생기.
/// 씬에 하나 두고 프리팹 10개를 연결한 뒤, 카드 시전 시 CardFXSpawner.Instance.Play(CardFX.Attack, 초상화RectTransform) 호출.
/// - 초상화 위치에 FX를 띄우고, 초상화 크기에 맞춰 자동 스케일 (FX 1px = 카드 1px 기준)
/// - Attack / DefenseBlock 은 초상화를 픽셀 단위로 흔들어 타격감 보강 (선택적으로 히트스톱)
/// - FX는 루트 ParticleSystem의 Stop Action = Destroy 로 스스로 사라짐
/// </summary>
public enum CardFX
{
    Attack, DefenseGain, DefenseBlock, Heal,
    BuffRegeneration, BuffForce, BuffLifesteal,
    DebuffPoison, DebuffBleed, DebuffWeaken,
}

public class CardFXSpawner : MonoBehaviour
{
    public static CardFXSpawner Instance { get; private set; }

    [Header("프리팹 (DualDuel_CRT_FX/Prefabs)")]
    public GameObject attack;
    public GameObject defenseGain;
    public GameObject defenseBlock;
    public GameObject heal;
    public GameObject buffRegeneration;
    public GameObject buffForce;
    public GameObject buffLifesteal;
    public GameObject debuffPoison;
    public GameObject debuffBleed;
    public GameObject debuffWeaken;

    [Header("크기")]
    [Tooltip("초상화 RectTransform 의 실제 크기에 맞춰 FX 스케일 자동 조절")]
    public bool fitToTarget = true;
    [Tooltip("FX가 기준으로 삼은 초상화 크기 (원본 픽셀). FX 캔버스는 96px, 초상화는 약 78px")]
    public float referencePortraitPx = 78f;
    [Tooltip("원본 1px 의 월드 크기. 200% export + PPU100 => 0.02")]
    public float worldUnitsPerNativePx = 0.02f;
    [Tooltip("FX를 카메라 쪽으로 당기는 거리 (Screen Space-Camera 캔버스 평면보다 앞에 오도록)")]
    public float zTowardCamera = 1f;

    [Header("타격감 (Attack / DefenseBlock)")]
    public bool shakeOnHit = true;
    [Tooltip("흔들림 진폭 (초상화 RectTransform 로컬 단위). 원본 2px 정도가 적당")]
    public float shakeAmplitude = 4f;
    public float shakeDuration = 0.15f;
    [Tooltip("타격 순간 Time.timeScale 을 0으로 잠깐 멈춤. 게임 흐름 전체가 멈추므로 기본 꺼둠")]
    public bool useHitStop = false;
    public float hitStopSeconds = 0.05f;

    // 흔들리는 중에 다시 맞아도 원래 위치를 잃지 않도록 대상별로 원위치와 코루틴을 기억
    readonly Dictionary<RectTransform, Vector2> _home = new Dictionary<RectTransform, Vector2>();
    readonly Dictionary<RectTransform, Coroutine> _shaking = new Dictionary<RectTransform, Coroutine>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public GameObject Prefab(CardFX fx) => fx switch
    {
        CardFX.Attack => attack,
        CardFX.DefenseGain => defenseGain,
        CardFX.DefenseBlock => defenseBlock,
        CardFX.Heal => heal,
        CardFX.BuffRegeneration => buffRegeneration,
        CardFX.BuffForce => buffForce,
        CardFX.BuffLifesteal => buffLifesteal,
        CardFX.DebuffPoison => debuffPoison,
        CardFX.DebuffBleed => debuffBleed,
        CardFX.DebuffWeaken => debuffWeaken,
        _ => null,
    };

    /// <summary>초상화(또는 아무 Transform) 위치에 FX 재생</summary>
    public GameObject Play(CardFX fx, Transform target)
    {
        GameObject prefab = Prefab(fx);
        if (prefab == null || target == null) { Debug.LogWarning($"[CardFX] {fx} 프리팹 또는 대상이 비어 있습니다."); return null; }

        Vector3 pos = target.position;
        Camera cam = Camera.main;
        if (cam != null) pos += (cam.transform.position - pos).normalized * zTowardCamera;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        if (fitToTarget && target is RectTransform rt) go.transform.localScale = Vector3.one * FitScale(rt);

        bool isHit = fx == CardFX.Attack || fx == CardFX.DefenseBlock;
        if (isHit && shakeOnHit && target is RectTransform shakeRt)
        {
            if (_shaking.TryGetValue(shakeRt, out Coroutine running) && running != null) StopCoroutine(running);
            else _home[shakeRt] = shakeRt.anchoredPosition;
            _shaking[shakeRt] = StartCoroutine(Shake(shakeRt));
        }
        if (isHit && useHitStop) StartCoroutine(HitStop());
        return go;
    }

    float FitScale(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        float worldWidth = Vector3.Distance(c[0], c[3]);
        float designWidth = referencePortraitPx * worldUnitsPerNativePx;
        return designWidth > 0f && worldWidth > 0f ? worldWidth / designWidth : 1f;
    }

    IEnumerator Shake(RectTransform rt)
    {
        Vector2 home = _home[rt];
        float t = 0f;
        int step = 0;
        while (t < shakeDuration)
        {
            float k = 1f - t / shakeDuration;
            // 픽셀 단위로 딱딱 끊기는 흔들림 (CRT/픽셀아트에 어울리게 보간 없음)
            float a = Mathf.Round(shakeAmplitude * k);
            Vector2 o = (step++ % 4) switch { 0 => new Vector2(a, 0), 1 => new Vector2(-a, a * 0.5f), 2 => new Vector2(a * 0.5f, -a * 0.5f), _ => new Vector2(-a, 0) };
            rt.anchoredPosition = home + o;
            yield return new WaitForSecondsRealtime(1f / 30f);
            t += 1f / 30f;
        }
        rt.anchoredPosition = home;
        _shaking.Remove(rt);
        _home.Remove(rt);
    }

    void OnDisable()
    {
        // 비활성화로 코루틴이 끊겨도 초상화는 제자리로
        foreach (var kv in _home) if (kv.Key != null) kv.Key.anchoredPosition = kv.Value;
        _home.Clear();
        _shaking.Clear();
    }

    IEnumerator HitStop()
    {
        float prev = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopSeconds);
        Time.timeScale = prev;
    }
}
