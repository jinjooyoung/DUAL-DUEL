using UnityEngine;

/// <summary>
/// GameManager의 현재 언어 설정을 기반으로 
/// DataManager에서 조회한 다국어 텍스트를 반환하는 매니저
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 번역 키에 해당하는 현재 언어의 문자열을 반환합니다.
    /// </summary>
    /// <param name="key">엑셀/SO에 정의된 고유 키 (예: "CARD_NAME_1001")</param>
    /// <returns>선택된 언어의 문자열 (데이터 누락 시 key 그대로 반환)</returns>
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;

        // 1. DataManager를 통해 LocalizationSO 조회
        LocalizationSO locData = DataManager.Instance != null ? DataManager.Instance.GetWord(key) : null;

        if (locData == null)
        {
            Debug.LogWarning($"[LocalizationManager] 번역 데이터를 찾을 수 없습니다: {key}");
            return $"{key}검색 실패";
        }

        // 2. GameManager의 언어 설정 참조 (없을 경우 기본값 KO)
        LanguageType currentLang = SettingsManager.Instance != null
            ? SettingsManager.Instance.languageType
            : LanguageType.KO;

        // 3. 언어 타입에 맞춰 해당 필드 텍스트 반환
        return currentLang switch
        {
            LanguageType.KO => !string.IsNullOrEmpty(locData.ko) ? locData.ko : locData.en,
            LanguageType.EN => !string.IsNullOrEmpty(locData.en) ? locData.en : locData.ko,
            LanguageType.JP => !string.IsNullOrEmpty(locData.jp) ? locData.jp : locData.ko,
            _ => locData.ko
        };
    }
}