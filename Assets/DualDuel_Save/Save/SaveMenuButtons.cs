using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 화면 저장 버튼. 메인 씬의 아무 오브젝트에 붙이고 이어하기 버튼 OnClick 에 OnClickContinue 를 연결하세요.
/// 저장이 없으면 이어하기 버튼을 비활성화합니다.
/// </summary>
public class SaveMenuButtons : MonoBehaviour
{
    [SerializeField] private Button continueButton;

    private void Start()
    {
        if (continueButton != null) continueButton.interactable = SaveManager.HasSave;
    }

    /// <summary>
    /// 이어하기 버튼 OnClick 에 연결: 저장을 불러오고 스테이지 선택 씬으로 이동
    /// </summary>
    public void OnClickContinue()
    {
        if (!SaveManager.ContinueGame() && continueButton != null) continueButton.interactable = false;
    }
}
