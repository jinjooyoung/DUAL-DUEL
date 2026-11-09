using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Global Volume에 붙어서 화면 밝기를 미세하게 흔들고(낡은 모니터 느낌),
/// 피격 등 순간에 Glitch()로 색수차/밝기를 튀게 하는 컴포넌트
/// 런타임에만 프로필 복사본을 수정하므로 원본 프로필 에셋은 바뀌지 않음
/// </summary>
[RequireComponent(typeof(Volume))]
public class CRTFlicker : MonoBehaviour
{
    public static CRTFlicker Instance { get; private set; }

    [Header("상시 밝기 흔들림")]
    [SerializeField, Range(0f, 0.3f)] private float exposureFlicker = 0.05f;
    [SerializeField] private float flickerSpeed = 6f;

    [Header("글리치 (피격 연출용)")]
    [SerializeField] private float glitchChromatic = 0.8f;
    [SerializeField] private float glitchExposure = 0.6f;

    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromatic;
    private float baseExposure;
    private float baseChromatic;
    private float glitchWeight;     // 0~1, 글리치 진행 정도
    private Coroutine glitchRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        VolumeProfile profile = GetComponent<Volume>().profile;   // 런타임 복사본
        if (profile.TryGet(out colorAdjustments)) baseExposure = colorAdjustments.postExposure.value;
        if (profile.TryGet(out chromatic)) baseChromatic = chromatic.intensity.value;
    }

    private void Update()
    {
        if (colorAdjustments != null)
        {
            float noise = (Mathf.PerlinNoise(0.37f, Time.unscaledTime * flickerSpeed) - 0.5f) * 2f;
            colorAdjustments.postExposure.value = baseExposure + noise * exposureFlicker + glitchWeight * glitchExposure;
        }

        if (chromatic != null)
        {
            chromatic.intensity.value = Mathf.Clamp01(baseChromatic + glitchWeight * glitchChromatic);
        }
    }

    /// <summary>
    /// 짧은 화면 글리치 (예: 플레이어 피격 시 CRTFlicker.Instance?.Glitch())
    /// </summary>
    public void Glitch(float duration = 0.18f)
    {
        if (glitchRoutine != null) StopCoroutine(glitchRoutine);
        glitchRoutine = StartCoroutine(Co_Glitch(duration));
    }

    private IEnumerator Co_Glitch(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            glitchWeight = 1f - t / duration;
            yield return null;
        }
        glitchWeight = 0f;
        glitchRoutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
