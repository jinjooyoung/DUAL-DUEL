using UnityEngine;
using UnityEngine.Events;
using static Unity.VisualScripting.Icons;

/// <summary>
/// 게임의 설정값(볼륨, 언어 등) 관리하는 싱글턴 매니저
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("언어 설정")]
    public LanguageType languageType = LanguageType.KO;
    public UnityEvent OnLanguageChanged;    // 언어가 변경되면 방송하는 이벤트

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

    /// <summary>
    /// 게임 내 언어 설정을 변경합니다.
    /// </summary>
    public void SetLanguage(LanguageType newLanguage)
    {
        if (languageType == newLanguage) return;

        languageType = newLanguage;

        OnLanguageChanged?.Invoke();
    }
}
