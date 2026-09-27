using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckViewerPopup : MonoBehaviour
{
    [Header("UI 바인딩")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform contentRoot;       // Viewport/Content
    [SerializeField] private GameObject viewerCardPrefab; // UI_ViewerCard 프리팹
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;

    // UI 오브젝트 풀 리스트
    private readonly List<UIViewerCard> cardUIPool = new List<UIViewerCard>();

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePopup);

        gameObject.SetActive(false);
    }

    // 1. 드로우 더미(Deck) 버튼 클릭 시
    public void OnClickDrawDeckButton()
    {
        if (BattleCardManager.Instance != null)
        {
            OpenViewer("뽑을 카드 더미", BattleCardManager.Instance.drawDeck);
        }
    }

    // 2. 버린 더미(Discard) 버튼 클릭 시
    public void OnClickDiscardDeckButton()
    {
        if (BattleCardManager.Instance != null)
        {
            OpenViewer("버린 카드 더미", BattleCardManager.Instance.discardDeck);
        }
    }

    /// <summary>
    /// 드로우 더미 또는 버린 더미 카드 목록 열기
    /// </summary>
    public void OpenViewer(string title, List<CardInstance> cards)
    {
        gameObject.SetActive(true);

        if (titleText != null)
            titleText.text = $"{title} ({cards.Count})";

        // 1. 필요한 수량만큼 풀에서 꺼내거나 새로 생성하여 세팅
        for (int i = 0; i < cards.Count; i++)
        {
            UIViewerCard cardUI = GetOrCreateCardUI(i);
            if (cardUI != null)
            {
                cardUI.gameObject.SetActive(true);
                cardUI.Setup(cards[i]);
            }
        }

        // 2. 카드 수보다 풀에 남는 여유 오브젝트는 비활성화(SetActive(false))
        for (int i = cards.Count; i < cardUIPool.Count; i++)
        {
            if (cardUIPool[i] != null)
            {
                cardUIPool[i].gameObject.SetActive(false);
            }
        }

        // 3. 스크롤 위치를 맨 위로 초기화
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
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