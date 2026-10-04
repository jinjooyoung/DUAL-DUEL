using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 게임의 설정값(볼륨, 언어 등) 관리하는 싱글턴 매니저
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("언어 설정")]
    public LanguageType languageType = LanguageType.KO;
    public UnityEvent OnLanguageChanged;    // 언어가 변경되면 방송하는 이벤트

    [Header("전투 속도 슬라이더")]
    [SerializeField] private Slider battleSpeedSlider;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 코드에서 직접 슬라이더 리스너를 달아주는 경우
        if (battleSpeedSlider != null)
        {
            battleSpeedSlider.onValueChanged.AddListener(OnBattleSpeedSliderChanged);
        }
    }

    /// <summary>
    /// 게임 내 언어 설정을 변경합니다.
    /// </summary>
    public void SetLanguage(LanguageType newLanguage)
    {
        if (languageType == newLanguage) return;

        languageType = newLanguage;

        OnLanguageChanged?.Invoke();
    }

    /// <summary>
    /// 슬라이더 조작 시 호출되는 함수 (인스펙터 UnityEvent 또는 AddListener로 바인딩)
    /// </summary>
    public void OnBattleSpeedSliderChanged(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.2f, 3.0f);

        // 현재 씬에 BattleManager가 있으면 딜레이 값 즉시 전달
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SetSlotActionDelay(clampedValue);
        }

        // (선택) 설정값 저장 필요 시: PlayerPrefs.SetFloat("BattleSlotDelay", clampedValue);
    }
}
