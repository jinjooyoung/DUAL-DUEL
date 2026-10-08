using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageInfoPanel : MonoBehaviour
{
    [Header("UI 텍스트 요소")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("시작 버튼")]
    [SerializeField] private Button startStageButton;

    private void Awake()
    {
        if (startStageButton != null)
        {
            startStageButton.onClick.AddListener(OnClickStartButton);
            startStageButton.interactable = false; // 최초 null일 때 비활성화
        }
    }

    /// <summary>
    /// 선택된 노드의 정보를 패널에 바인딩
    /// </summary>
    public void SetupPanel(StageNode node)
    {
        gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = GetStageTitle(node.stageType);
        }

        if (descriptionText != null)
        {
            descriptionText.text = GetStageDescription(node.stageType);
        }

        if (startStageButton != null)
        {
            startStageButton.interactable = (node != null && node.canGo);
        }
    }

    public void SetStartButtonInteractable(bool interactable)
    {
        if (startStageButton != null)
        {
            startStageButton.interactable = interactable;
        }
    }

    private void OnClickStartButton()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.StartCurrentSelectedStage();
        }
    }

    private string GetStageTitle(StageType type)
    {
        switch (type)
        {
            case StageType.Normal: return "일반 전투";
            case StageType.Elite: return "엘리트 전투";
            case StageType.Boss: return "보스 전투";
            case StageType.Rest: return "정비소";
            case StageType.Shop: return "상점";
            case StageType.Random: return "미지의 구역";
            default: return "알 수 없음";
        }
    }

    private string GetStageDescription(StageType type)
    {
        switch (type)
        {
            case StageType.Normal: return "일반 몬스터와 전투를 진행합니다.";
            case StageType.Elite: return "강력한 엘리트 몬스터를 처치하고 더 큰 보상을 얻습니다.";
            case StageType.Boss: return "구역의 최종 보스와 결전을 치릅니다.";
            case StageType.Rest: return "휴식을 취하거나 덱의 카드를 정비(강화/삭제)합니다.";
            case StageType.Shop: return "골드를 소모하여 카드 및 아이템을 구매합니다.";
            case StageType.Random: return "어떤 조우가 일어날지 알 수 없는 미지의 장소입니다.";
            default: return "";
        }
    }
}