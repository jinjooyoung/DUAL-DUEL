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

    private readonly List<GameObject> spawnedCards = new List<GameObject>();

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

        // 기존 생성된 카드 UI 정리
        foreach (var cardObj in spawnedCards)
        {
            Destroy(cardObj);
        }
        spawnedCards.Clear();

        // 새 카드 UI 인스턴스화 및 배치
        if (viewerCardPrefab != null && contentRoot != null)
        {
            foreach (var card in cards)
            {
                if (card == null) continue;
                GameObject newCard = Instantiate(viewerCardPrefab, contentRoot);
                UIViewerCard cardUI = newCard.GetComponent<UIViewerCard>();
                if (cardUI != null)
                {
                    cardUI.Setup(card);
                }
                spawnedCards.Add(newCard);
            }
        }

        // 스크롤 위치를 맨 위로 초기화
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}