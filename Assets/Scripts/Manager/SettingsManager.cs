using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("전투 속도 슬라이더")]
    [SerializeField] private Slider battleSpeedSlider;

    [Header("설정 팝업 패널")]
    [SerializeField] private GameObject settingPopup;

    [Header("언어 설정 드롭다운")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    // 드롭다운 순서와 일치
    private readonly string[] localeCodes = { "en", "ko", "ja" };
    private readonly string[] localeNames = { "English", "Korean", "Japanese" };

    private bool isChangingLanguage = false;
    private bool isInitialized = false;

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
        if (battleSpeedSlider != null)
        {
            battleSpeedSlider.onValueChanged.AddListener(
                OnBattleSpeedSliderChanged);
        }

        if (settingPopup != null) settingPopup.SetActive(false);

        StartCoroutine(InitializeLanguageDropdown());
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (battleSpeedSlider != null)
        {
            battleSpeedSlider.onValueChanged.RemoveListener(
                OnBattleSpeedSliderChanged);
        }

        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveListener(
                OnLanguageDropdownChanged);
        }

        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private IEnumerator InitializeLanguageDropdown()
    {
        // Localization 초기화 대기
        yield return LocalizationSettings.InitializationOperation;

        if (languageDropdown != null)
        {
            // 드롭다운 항목 구성
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new System.Collections.Generic.List<string>(
                localeNames));

            languageDropdown.onValueChanged.RemoveListener(
                OnLanguageDropdownChanged);

            languageDropdown.onValueChanged.AddListener(
                OnLanguageDropdownChanged);
        }

        // 외부에서 언어를 변경해도 드롭다운 동기화
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

        isInitialized = true;

        // 현재 선택된 언어를 드롭다운에 반영
        UpdateDropdownSelection(LocalizationSettings.SelectedLocale);
    }

    /// <summary>
    /// 드롭다운에서 선택한 언어로 변경
    /// </summary>
    private void OnLanguageDropdownChanged(int index)
    {
        if (!isInitialized || isChangingLanguage) return;
        if (index < 0 || index >= localeCodes.Length) return;

        SetLanguage(localeCodes[index]);
    }

    /// <summary>
    /// 언어 코드로 변경 (예: en, ko, ja)
    /// </summary>
    public void SetLanguage(string localeCode)
    {
        if (isChangingLanguage) return;

        StartCoroutine(ChangeLocaleRoutine(localeCode));
    }

    private IEnumerator ChangeLocaleRoutine(string localeCode)
    {
        isChangingLanguage = true;

        yield return LocalizationSettings.InitializationOperation;

        Locale targetLocale = LocalizationSettings.AvailableLocales.Locales
            .Find(locale =>
                string.Equals(
                    locale.Identifier.Code,
                    localeCode,
                    System.StringComparison.OrdinalIgnoreCase));

        if (targetLocale != null)
        {
            LocalizationSettings.SelectedLocale = targetLocale;

            Debug.Log(
                $"[SettingsManager] 언어 변경 완료: {targetLocale.LocaleName}");
        }
        else
        {
            Debug.LogWarning(
                $"[SettingsManager] 등록되지 않은 언어 코드: {localeCode}");
        }

        isChangingLanguage = false;

        // 선택 상태 동기화
        UpdateDropdownSelection(LocalizationSettings.SelectedLocale);
    }

    /// <summary>
    /// 다른 코드에서 언어를 변경했을 때도 호출
    /// </summary>
    private void OnSelectedLocaleChanged(Locale locale)
    {
        UpdateDropdownSelection(locale);
    }

    /// <summary>
    /// 현재 언어에 맞춰 드롭다운 선택값 갱신
    /// </summary>
    private void UpdateDropdownSelection(Locale locale)
    {
        if (languageDropdown == null || locale == null) return;

        string currentCode = locale.Identifier.Code;

        for (int i = 0; i < localeCodes.Length; i++)
        {
            if (string.Equals(
                currentCode,
                localeCodes[i],
                System.StringComparison.OrdinalIgnoreCase))
            {
                // 값 변경 이벤트를 다시 발생시키지 않음
                languageDropdown.SetValueWithoutNotify(i);
                languageDropdown.RefreshShownValue();
                return;
            }
        }

        Debug.LogWarning(
            $"[SettingsManager] 드롭다운에 등록되지 않은 언어: {currentCode}");
    }

    /// <summary>
    /// 기존 인덱스 기반 언어 변경 기능
    /// 0: English, 1: Korean, 2: Japanese
    /// </summary>
    public void SetLanguageByIndex(int index)
    {
        if (index < 0 || index >= localeCodes.Length) return;

        SetLanguage(localeCodes[index]);
    }

    /// <summary>
    /// 전투 속도 슬라이더
    /// </summary>
    public void OnBattleSpeedSliderChanged(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.2f, 3.0f);

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SetSlotActionDelay(clampedValue);
        }
    }

    /// <summary>
    /// 세팅 팝업 토글 온오프
    /// </summary>
    public void TogglePopup()
    {
        if (settingPopup != null) settingPopup.SetActive(!settingPopup.activeSelf);
    }
}