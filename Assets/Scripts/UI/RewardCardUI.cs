using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardCardUI : MonoBehaviour
{
    [Header("카드 3장 UI 뷰어")]
    [SerializeField] private List<UIViewerCard> cardViewers = new List<UIViewerCard>();

    [Header("카드 선택 버튼")]
    [SerializeField] private List<Button> cardSelectButtons = new List<Button>();

    [Header("카드별 하단 새로고침 버튼")]
    [SerializeField] private List<Button> rerollButtons = new List<Button>();

    [Header("남은 새로고침 횟수 텍스트")]
    [SerializeField] private TextMeshProUGUI remainRerollText;

    // 이번 보상 창에서 이미 새로고침을 진행한 슬롯 플래그
    private bool[] isSlotRerolled = new bool[3];

    private void Awake()
    {
        // 카드 선택 버튼 이벤트 바인딩 (인덱스 고정 캡처)
        for (int i = 0; i < cardSelectButtons.Count; i++)
        {
            int idx = i;
            cardSelectButtons[i].onClick.AddListener(() => OnClickCardSelect(idx));
        }

        // 새로고침 버튼 이벤트 바인딩
        for (int i = 0; i < rerollButtons.Count; i++)
        {
            int idx = i;
            rerollButtons[i].onClick.AddListener(() => OnClickReroll(idx));
        }
    }

    /// <summary>
    /// 전투 승리 시 호출되어 보상 카드를 생성하고 UI를 엽니다.
    /// </summary>
    public void OpenRewardPanel()
    {
        gameObject.SetActive(true);

        // 이번 보상 패널 슬롯별 리롤 상태 초기화
        for (int i = 0; i < isSlotRerolled.Length; i++)
        {
            isSlotRerolled[i] = false;
        }

        // 1. RewardManager에 3장 추첨 명령
        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.GenerateRewardCards();
        }

        // 2. UI 전체 세팅
        UpdateAllCardUI();
        UpdateRerollButtonsInteractable();
    }

    /// <summary>
    /// 3장의 카드 뷰어 데이터를 갱신
    /// </summary>
    private void UpdateAllCardUI()
    {
        if (RewardManager.Instance == null) return;

        List<CardSO> cards = RewardManager.Instance.currentRewardCards;

        for (int i = 0; i < cardViewers.Count; i++)
        {
            if (i < cards.Count && cards[i] != null)
            {
                // UIViewerCard에 CardInstance 형태로 전달
                CardInstance tempInstance = new CardInstance(cards[i], 0);
                cardViewers[i].Setup(tempInstance);
                cardSelectButtons[i].interactable = true;
            }
            else
            {
                cardSelectButtons[i].interactable = false;
            }
        }

        UpdateRerollText();
    }

    /// <summary>
    /// 단일 슬롯 UI만 갱신
    /// </summary>
    private void UpdateSingleCardUI(int index)
    {
        if (RewardManager.Instance == null) return;

        if (index >= 0 && index < RewardManager.Instance.currentRewardCards.Count)
        {
            CardSO card = RewardManager.Instance.currentRewardCards[index];
            CardInstance tempInstance = new CardInstance(card, 0);
            cardViewers[index].Setup(tempInstance);
        }

        UpdateRerollText();
        UpdateRerollButtonsInteractable();
    }

    private void UpdateRerollText()
    {
        if (remainRerollText != null && RewardManager.Instance != null)
        {
            remainRerollText.text = $"남은 새로고침 횟수 : {RewardManager.Instance.remainRerollCount}";
        }
    }

    private void UpdateRerollButtonsInteractable()
    {
        if (RewardManager.Instance == null) return;

        bool hasGlobalReroll = RewardManager.Instance.remainRerollCount > 0;

        for (int i = 0; i < rerollButtons.Count; i++)
        {
            // 남은 전역 횟수가 있고 && 해당 슬롯을 아직 새로고침 안 했을 때만 활성화
            rerollButtons[i].interactable = hasGlobalReroll && !isSlotRerolled[i];
        }
    }

    /// <summary>
    /// 카드 선택 시 처리
    /// </summary>
    private void OnClickCardSelect(int index)
    {
        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.SelectCard(index);
        }

        gameObject.SetActive(false);

        // 보상 획득 후 스테이지 복귀
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ReturnToStageSelect();
        }
    }

    /// <summary>
    /// 새로고침 버튼 클릭 시 처리
    /// </summary>
    private void OnClickReroll(int index)
    {
        if (RewardManager.Instance == null) return;

        bool success = RewardManager.Instance.RerollCardSlot(index);
        if (success)
        {
            isSlotRerolled[index] = true; // 해당 카드는 이번 보상에서 더 이상 리롤 불가
            UpdateSingleCardUI(index);
        }
    }
}