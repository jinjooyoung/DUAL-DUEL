using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 보유 덱(GameManager.Instance.deck) 목록 팝업.
/// 전투 중 드로우/버린 더미를 보여주는 DeckViewerPopup 과 같은 구조이고,
/// 보여주는 덱만 BattleCardManager 대신 GameManager 에서 가져온다 (상점, 휴식, 스테이지 선택 등 전투 밖에서 사용).
/// </summary>
public class OwnedDeckViewerPopup : MonoBehaviour
{
    [Header("UI 바인딩")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform contentRoot;       // Viewport/Content
    [SerializeField] private GameObject viewerCardPrefab; // UI_ViewerCard 프리팹
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;

    [Header("표시")]
    [SerializeField] private string title = "보유 덱";

    // UI 오브젝트 풀 리스트
    private readonly List<UIViewerCard> cardUIPool = new List<UIViewerCard>();

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePopup);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 덱 보기 버튼 클릭 시 (버튼의 OnClick 에 연결)
    /// </summary>
    public void OnClickOwnedDeckButton()
    {
        if (GameManager.Instance != null)
        {
            OpenViewer(title, GameManager.Instance.deck);
        }
    }

    /// <summary>
    /// 카드 목록 열기
    /// 원본 덱은 유지하고, UI 표시용 복사본만 정렬
    /// </summary>
    public void OpenViewer(string title, List<CardInstance> cards)
    {
        gameObject.SetActive(true);

        // 1. 원본 덱을 복사한 임시 리스트 생성
        List<CardInstance> tempCards = cards != null ? new List<CardInstance>(cards) : new List<CardInstance>();

        // 2. 임시 리스트 정렬
        SortCardsForViewer(tempCards);

        if (titleText != null)
            titleText.text = $"{title} ({tempCards.Count})";

        // 3. 정렬된 임시 리스트를 UI에 표시
        for (int i = 0; i < tempCards.Count; i++)
        {
            UIViewerCard cardUI = GetOrCreateCardUI(i);
            if (cardUI != null)
            {
                cardUI.gameObject.SetActive(true);
                cardUI.Setup(tempCards[i]);
            }
        }

        // 4. 카드 수보다 풀에 남는 여유 오브젝트는 비활성화
        for (int i = tempCards.Count; i < cardUIPool.Count; i++)
        {
            if (cardUIPool[i] != null)
            {
                cardUIPool[i].gameObject.SetActive(false);
            }
        }

        // 5. 스크롤 위치를 맨 위로 초기화
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// 카드 목록 UI 표시용 정렬 (DeckViewerPopup 과 같은 기준)
    /// 1순위: ConditionCategory
    /// 2순위: CardType
    /// 3순위: RankType
    /// 4순위: CardId
    /// </summary>
    private void SortCardsForViewer(List<CardInstance> cards)
    {
        cards.Sort((a, b) =>
        {
            // 유효하지 않은 카드 처리
            if (a == null || a.baseData == null)
            {
                if (b == null || b.baseData == null) return 0;
                return 1;
            }

            if (b == null || b.baseData == null)
                return -1;

            CardSO dataA = a.baseData;
            CardSO dataB = b.baseData;

            // 1. ConditionCategory 오름차순
            int conditionCompare = ((int)dataA.conditionCategory)
                .CompareTo((int)dataB.conditionCategory);

            if (conditionCompare != 0)
                return conditionCompare;

            // 2. CardType 오름차순
            int typeCompare = ((int)dataA.cardType)
                .CompareTo((int)dataB.cardType);

            if (typeCompare != 0)
                return typeCompare;

            // 3. RankType 오름차순
            int rankCompare = ((int)dataA.rank)
                .CompareTo((int)dataB.rank);

            if (rankCompare != 0)
                return rankCompare;

            // 4. CardId 오름차순
            return dataA.cardId.CompareTo(dataB.cardId);
        });
    }

    /// <summary>
    /// 풀에서 인덱스에 해당하는 UI를 가져오거나, 없으면 새로 인스턴스화
    /// </summary>
    private UIViewerCard GetOrCreateCardUI(int index)
    {
        // 이미 풀에 생성되어 있는 경우
        if (index < cardUIPool.Count)
        {
            return cardUIPool[index];
        }

        // 부족할 경우 새로 생성하여 풀에 추가
        if (viewerCardPrefab != null && contentRoot != null)
        {
            GameObject newObj = Instantiate(viewerCardPrefab, contentRoot);
            UIViewerCard cardUI = newObj.GetComponent<UIViewerCard>();
            if (cardUI != null)
            {
                cardUIPool.Add(cardUI);
                return cardUI;
            }
        }

        return null;
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}
