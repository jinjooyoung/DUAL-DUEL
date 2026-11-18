using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 씬 중앙 스크립트.
///   입장 -> 추첨기가 없으면 Initialize -> 8번 반복 (랭크 추첨 -> 그 랭크의 카드 -> i번째 UIViewerCard 에 세팅, 어떤 CardSO 인지 기억)
///   카드 클릭 -> 그 CardSO 로 CardInstance 를 만들어 GameManager.deck 에 추가, 골드 차감, 다른 카드 구매 가능 여부 갱신, 구매 연출
///   나가기 -> StageManager.Instance.ReturnToStageSelect()
///
/// 상점 추첨기는 RewardManager 의 보상 추첨기와 따로 누적된다 (고등급 보정이 상점에서 터졌는데 돈이 없어 못 사는 일을 보상과 분리).
/// 상점 씬 오브젝트는 씬을 나가면 사라지므로, 누적이 유지되도록 추첨기는 static 으로 둔다.
/// 모든 참조는 Tools > DUAL DUEL > 상점 씬 구성 툴이 채운다.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public const int SlotCount = 8;

    [Header("카드 8장 (씬에 미리 배치된 UIViewerCard. 순서 = 윗줄 왼쪽 -> 오른쪽, 아랫줄 왼쪽 -> 오른쪽)")]
    [SerializeField] private List<UIViewerCard> cardViewers = new List<UIViewerCard>();
    [Tooltip("카드별 가격 텍스트 (cardViewers 와 같은 순서)")]
    [SerializeField] private List<TMP_Text> priceTexts = new List<TMP_Text>();
    [Tooltip("카드별 가격 옆 코인 아이콘 (가격과 함께 어두워지고 사라짐, 없어도 됨)")]
    [SerializeField] private List<Graphic> priceIcons = new List<Graphic>();

    [Header("UI")]
    [SerializeField] private GoldCounter goldCounter;
    [SerializeField] private Button exitButton;
    [Tooltip("구매한 카드가 날아가 들어가는 곳 (좌하단 덱 보기 버튼)")]
    [SerializeField] private RectTransform deckTarget;
    [Tooltip("덱 보기 버튼 (없어도 됨)")]
    [SerializeField] private Button deckButton;
    [Tooltip("덱 보기에 쓸 DeckViewerPopup (씬에 있으면 연결, 없으면 버튼은 연출 도착 지점으로만 쓰임)")]
    [SerializeField] private DeckViewerPopup deckViewer;
    [SerializeField] private ShopPurchaseFX purchaseFX;

    [Header("표시")]
    [Tooltip("살 수 없는 카드의 밝기 (0 = 검정, 1 = 원래 밝기)")]
    [Range(0f, 1f)] [SerializeField] private float unaffordableBrightness = 0.4f;
    [SerializeField] private Color priceColor = new Color(0.75f, 1f, 0.8f);

    // 상점 전용 랭크 추첨기 (보상 추첨기와 별개로 누적). 씬이 바뀌어도 누적이 유지되도록 static
    private static SoftPityProbabilityCorrector<RankType> corrector;
    private static bool isInitialized;

    // 이번 입장에서 진열된 카드와 판매 여부
    private readonly CardSO[] shopCards = new CardSO[SlotCount];
    private readonly bool[] sold = new bool[SlotCount];
    private readonly List<ShopCardSlot> slots = new List<ShopCardSlot>();

    private static int Gold => GameManager.Instance != null ? GameManager.Instance.currentGold : 0;

    // ------------------------------------------------------------------ 추첨기

    public void Initialize()
    {
        if (isInitialized) return;
        if (RewardManager.Instance == null)
        {
            Debug.LogError("<color=red>[ShopManager] RewardManager.Instance 가 없어 가중치 테이블을 가져올 수 없습니다.</color>");
            return;
        }

        corrector = new SoftPityProbabilityCorrector<RankType>(RewardManager.Instance.chapter1Weights, (int)RankType.Rare);
        isInitialized = true;
        Debug.Log("<color=green>[ShopManager] 상점 제시 카드 추첨기 초기화 완료</color>");
    }

    /// <summary>챕터가 바뀔 때 상점 추첨기의 가중치 테이블도 바꿈 (RewardManager.SetChapter 와 같은 테이블)</summary>
    public void SetChapter(int chapter)
    {
        if (corrector == null) Initialize();
        if (corrector == null) return;

        switch (chapter)
        {
            case 1: corrector.SetWeightTable(RewardManager.Instance.chapter1Weights); break;
            case 2: corrector.SetWeightTable(RewardManager.Instance.chapter2Weights); break;
            case 3: corrector.SetWeightTable(RewardManager.Instance.chapter3Weights); break;
        }
    }

    /// <summary>새 게임을 시작할 때 상점 누적을 지움 (GameManager.DeckInit / StatInit 과 함께 호출)</summary>
    public static void ResetShopRoll()
    {
        corrector = null;
        isInitialized = false;
    }

    // 에디터에서 Domain Reload 를 끈 채 플레이를 다시 시작해도 이전 누적이 남지 않게
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => ResetShopRoll();

    /// <summary>RewardManager.EvaluateRank 와 같은 방식 (상점 추첨기 사용)</summary>
    public RankType EvaluateRank()
    {
        if (corrector == null) Initialize();
        return corrector != null ? corrector.EvaluateNext() : RankType.Common;
    }

    // ------------------------------------------------------------------ 입장

    private void Start()
    {
        Initialize();
        BindSlots();
        GenerateShopCards();

        if (goldCounter != null) goldCounter.SetInstant(Gold);
        RefreshInteractable();

        if (exitButton != null) exitButton.onClick.AddListener(OnClickExit);
        if (deckButton != null) deckButton.onClick.AddListener(OnClickDeck);
    }

    /// <summary>카드마다 클릭(누름/뗌 연출 + 구매)을 연결. RewardCardUI 처럼 인덱스를 고정 캡처</summary>
    private void BindSlots()
    {
        slots.Clear();
        for (int i = 0; i < cardViewers.Count; i++)
        {
            if (cardViewers[i] == null) { slots.Add(null); continue; }
            ShopCardSlot slot = cardViewers[i].GetComponent<ShopCardSlot>();
            if (slot == null) slot = cardViewers[i].gameObject.AddComponent<ShopCardSlot>();
            int idx = i;
            slot.Init(() => OnClickCardSelect(idx));
            slots.Add(slot);
        }
    }

    /// <summary>8장 추첨 후 바로 세팅 (연출 없음: 입장하자마자 8장이 보임)</summary>
    public void GenerateShopCards()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            RankType rolledRank = EvaluateRank();
            CardSO card = RewardManager.Instance != null ? RewardManager.Instance.GetRandomCardByRank(rolledRank) : null;

            // 안전장치: 비어 있으면 Common 으로 한 번 더
            if (card == null && RewardManager.Instance != null) card = RewardManager.Instance.GetRandomCardByRank(RankType.Common);

            shopCards[i] = card;
            sold[i] = false;

            if (i < cardViewers.Count && cardViewers[i] != null)
            {
                cardViewers[i].gameObject.SetActive(card != null);
                if (card != null) cardViewers[i].Setup(new CardInstance(card, 0));
            }
            if (i < priceTexts.Count && priceTexts[i] != null)
            {
                priceTexts[i].gameObject.SetActive(card != null);
                if (card != null) priceTexts[i].text = card.price.ToString();
            }
            if (i < priceIcons.Count && priceIcons[i] != null) priceIcons[i].gameObject.SetActive(card != null);

            Debug.Log($"<color=cyan>[ShopManager] {i}번 슬롯: {card?.nameKey} (등급: {rolledRank}, 가격: {card?.price})</color>");
        }
    }

    // ------------------------------------------------------------------ 구매

    private void OnClickCardSelect(int idx)
    {
        if (idx < 0 || idx >= SlotCount || sold[idx]) return;
        CardSO card = shopCards[idx];
        if (card == null || GameManager.Instance == null) return;
        if (card.price > Gold) return;    // 버튼이 막혀 있지만 한 번 더 확인

        // 1. 덱에 추가, 골드 차감
        GameManager.Instance.deck.Add(new CardInstance(card, 0));
        GameManager.Instance.currentGold -= card.price;
        sold[idx] = true;
        Debug.Log($"<color=green>[ShopManager] 구매: {card.nameKey} (-{card.price}, 남은 골드 {Gold}, 덱 {GameManager.Instance.deck.Count}장)</color>");

        // 2. 가격 표시 제거, 골드 롤링
        if (idx < priceTexts.Count && priceTexts[idx] != null) priceTexts[idx].gameObject.SetActive(false);
        if (idx < priceIcons.Count && priceIcons[idx] != null) priceIcons[idx].gameObject.SetActive(false);
        if (goldCounter != null) goldCounter.RollTo(Gold);
        if (TooltipPanel.Instance != null) TooltipPanel.Instance.HideTooltip();

        // 3. 구매 연출 (띠용 -> 빙글빙글 -> 번쩍 -> 덱으로)
        ShopCardSlot slot = slots[idx];
        if (slot != null) slot.SetSold();
        if (purchaseFX != null && slot != null)
            purchaseFX.Play((RectTransform)slot.transform, slot.OriginalScale, deckTarget);
        else if (slot != null) slot.gameObject.SetActive(false);

        // 4. 남은 카드들의 구매 가능 여부 갱신
        RefreshInteractable();
    }

    /// <summary>모든 카드의 Interactable 을 (가격 <= 현재 골드) 로 갱신. 살 수 없으면 카드와 가격이 함께 어두워짐</summary>
    public void RefreshInteractable()
    {
        int gold = Gold;
        for (int i = 0; i < SlotCount; i++)
        {
            if (sold[i] || shopCards[i] == null) continue;
            bool canBuy = shopCards[i].price <= gold;

            if (i < slots.Count && slots[i] != null) slots[i].SetAffordable(canBuy, unaffordableBrightness);

            Color c = canBuy ? priceColor : Dim(priceColor);
            if (i < priceTexts.Count && priceTexts[i] != null) priceTexts[i].color = c;
            if (i < priceIcons.Count && priceIcons[i] != null) priceIcons[i].color = canBuy ? Color.white : Dim(Color.white);
        }
    }

    private Color Dim(Color c) => new Color(c.r * unaffordableBrightness, c.g * unaffordableBrightness, c.b * unaffordableBrightness, c.a);

    // ------------------------------------------------------------------ 나가기 / 덱 보기

    private void OnClickExit()
    {
        if (StageManager.Instance != null) StageManager.Instance.ReturnToStageSelect();
        else Debug.LogWarning("[ShopManager] StageManager.Instance 가 없어 스테이지로 돌아갈 수 없습니다.");
    }

    private void OnClickDeck()
    {
        if (deckViewer != null && GameManager.Instance != null)
            deckViewer.OpenViewer("보유 덱", GameManager.Instance.deck);
    }
}
