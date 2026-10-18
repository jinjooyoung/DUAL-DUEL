using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬에 배치하여 인스펙터 드롭다운으로 선택한 BGM을 자동으로 재생하는 컴포넌트
/// </summary>
public class BGMPlayer : MonoBehaviour
{
    [Header("재생할 BGM 타입")]
    [SerializeField] private BGMType bgmType = BGMType.Title;

    [Header("페이드 인/아웃 시간 (초)")]
    [Range(0f, 3f)]
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("옵션")]
    [Tooltip("체크 시 이전 씬과 동일한 BGM이더라도 처음부터 다시 재생")]
    [SerializeField] private bool restartIfSame = false;

    private void Start()
    {
        PlayBGM();
    }

    /// <summary>
    /// 인스펙터에서 지정된 BGM을 SoundManager를 통해 재생합니다.
    /// </summary>
    public void PlayBGM()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[BGMPlayer] SoundManager.Instance를 찾을 수 없습니다.");
            return;
        }

        // 동일한 BGM이 이미 재생 중이고 강제 재시작 옵션이 꺼져 있으면 스킵
        if (!restartIfSame && SoundManager.Instance.CurrentBGM == bgmType)
        {
            return;
        }

        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "BattleScene")
        {
            StageType currentType = StageManager.Instance.currentNode.stageType;

            if (currentType == StageType.Normal || currentType == StageType.Elite)
            {
                SoundManager.Instance.PlayBGM(BGMType.Battle_Normal, fadeDuration);
                return;
            }
            else
            {
                SoundManager.Instance.PlayBGM(BGMType.Battle_Boss, fadeDuration);
                return;
            }
        }

        SoundManager.Instance.PlayBGM(bgmType, fadeDuration);
    }

    /// <summary>
    /// 런타임에 코드로 BGM 타입을 교체하고 싶을 때 호출
    /// </summary>
    public void ChangeBGM(BGMType newBGM, float customFadeDuration = -1f)
    {
        bgmType = newBGM;
        float duration = customFadeDuration >= 0f ? customFadeDuration : fadeDuration;
        SoundManager.Instance?.PlayBGM(bgmType, duration);
    }
}