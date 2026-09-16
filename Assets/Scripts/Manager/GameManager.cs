using UnityEngine;

/// <summary>
/// 게임 전역 상태 및 환경 설정(언어 등)을 관리하는 싱글턴 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("언어 설정")]
    public LanguageType languageType = LanguageType.KO;

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
    /// 게임 내 언어 설정을 변경합니다.
    /// </summary>
    public void SetLanguage(LanguageType newLanguage)
    {
        languageType = newLanguage;
        // 텍스트 싹다 새로고침하는 함수 호출
    }
}
