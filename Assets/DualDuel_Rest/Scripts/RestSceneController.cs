using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 휴식 씬 화면 흐름.
///   선택 화면 (회복 / 강화 / 제거)
///   회복 -> 팝업 (회복하기 / 돌아가기) -> 회복 컷씬
///   강화 -> 보유 덱 목록 -> 카드 클릭 -> 강화 전/후 비교 (강화하기 / 돌아가기) -> 강화 컷씬
///   제거 -> 보유 덱 목록 -> 카드 클릭 -> 카드 1장 (제거하기 / 돌아가기) -> 제거 컷씬
///
/// 카드 목록은 DeckViewerPopup 과 같은 방식: 보유 덱(bridge.GetDeck)을 복사해 정렬하고,
/// UI_ViewerCard 프리팹 풀을 재사용해 Setup(CardInstance) 로 채운다.
/// 카드 클릭은 RewardCardUI 와 같은 방식: 버튼마다 인덱스를 고정 캡처해 OnClickCardSelect(idx) 호출.
/// </summary>
public class RestSceneController : MonoBehaviour
{
    enum Mode { Upgrade, Remove }

    public RestSceneBridge bridge;
    public RestCutscenes cutscenes;

    [Header("선택 화면")]
    public GameObject selection;
    public Button healOption, upgradeOption, removeOption;

    [Header("회복 팝업")]
    public GameObject healPopup;
    public TMP_Text healText;
    public Button healConfirm, healCancel;

    [Header("카드 목록")]
    public GameObject listOverlay;
    public TMP_Text listTitle;
    public RectTransform listContent;          // Viewport/Content (GridLayoutGroup)
    public ScrollRect listScroll;
    public Button listClose;
    [Tooltip("UI_ViewerCard 프리팹 (DeckViewerPopup 에서 쓰는 것과 같은 프리팹). 비우면 bridge.viewerCardPrefab 사용")]
    public GameObject viewerCardPrefab;

    [Header("강화 비교")]
    public GameObject comparePopup;
    public RectTransform compareBefore, compareAfter;
    public Button upgradeConfirm, upgradeCancel;

    [Header("제거 확인")]
    public GameObject removePopup;
    public RectTransform removeSlot;
    public Button removeConfirm, removeCancel;

    Mode _mode;
    CardInstance _picked;
    RectTransform _pickedView;
    bool _busy;

    // 지금 목록에 보이는 카드 (정렬된 복사본). 버튼의 인덱스가 이 리스트를 가리킨다
    readonly List<CardInstance> _listCards = new List<CardInstance>();
    // 목록용 UI 오브젝트 풀 (DeckViewerPopup.cardUIPool 과 같은 역할)
    readonly List<UIViewerCard> _cardUIPool = new List<UIViewerCard>();

    GameObject CardPrefab => viewerCardPrefab != null ? viewerCardPrefab : (bridge != null ? bridge.viewerCardPrefab : null);

    void Start()
    {
        healOption.onClick.AddListener(OpenHeal);
        upgradeOption.onClick.AddListener(() => OpenList(Mode.Upgrade));
        removeOption.onClick.AddListener(() => OpenList(Mode.Remove));
        healConfirm.onClick.AddListener(ConfirmHeal);
        healCancel.onClick.AddListener(() => healPopup.SetActive(false));
        listClose.onClick.AddListener(() => { HideTooltip(); listOverlay.SetActive(false); });
        upgradeConfirm.onClick.AddListener(ConfirmUpgrade);
        upgradeCancel.onClick.AddListener(() => comparePopup.SetActive(false));
        removeConfirm.onClick.AddListener(ConfirmRemove);
        removeCancel.onClick.AddListener(() => removePopup.SetActive(false));

        selection.SetActive(true);
        healPopup.SetActive(false);
        listOverlay.SetActive(false);
        comparePopup.SetActive(false);
        removePopup.SetActive(false);
    }

    // ------------------------------------------------------------------ heal

    void OpenHeal()
    {
        if (_busy || bridge == null) return;
        int pct = Mathf.RoundToInt(bridge.healRatio * 100f);
        healText.text = $"최대 체력의 {pct}%를 회복합니다.\n{bridge.CurrentHP}  ->  {bridge.HPAfterHeal}";
        healPopup.SetActive(true);
    }

    void ConfirmHeal()
    {
        if (_busy) return;
        _busy = true;
        healPopup.SetActive(false);
        selection.SetActive(false);
        StartCoroutine(cutscenes.PlayHeal());
    }

    // ------------------------------------------------------------------ card list

    void OpenList(Mode mode)
    {
        if (_busy || bridge == null) return;
        _mode = mode;

        MatchGridToPrefab();

        // 1. 보유 덱을 복사한 임시 리스트 (강화는 강화 가능한 카드만)
        _listCards.Clear();
        List<CardInstance> deck = bridge.GetDeck();
        if (deck != null)
            foreach (CardInstance card in deck)
                if (mode == Mode.Remove || bridge.CanUpgrade(card)) _listCards.Add(card);

        // 2. 임시 리스트 정렬 (DeckViewerPopup 과 같은 기준)
        SortCardsForViewer(_listCards);

        string title = mode == Mode.Upgrade ? "강화할 카드를 선택하세요" : "제거할 카드를 선택하세요";
        listTitle.text = $"{title} ({_listCards.Count})";

        // 3. 정렬된 리스트를 UI 에 표시
        for (int i = 0; i < _listCards.Count; i++)
        {
            UIViewerCard cardUI = GetOrCreateCardUI(i);
            if (cardUI == null) continue;
            cardUI.gameObject.SetActive(true);
            cardUI.Setup(_listCards[i]);
        }

        // 4. 카드 수보다 풀에 남는 오브젝트는 비활성화
        for (int i = _listCards.Count; i < _cardUIPool.Count; i++)
            if (_cardUIPool[i] != null) _cardUIPool[i].gameObject.SetActive(false);

        listOverlay.SetActive(true);

        // 5. 스크롤 위치를 맨 위로
        if (listScroll != null) listScroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// 그리드 칸 크기를 UI_ViewerCard 프리팹 크기에 맞춤.
    /// 그리드는 칸 크기로 카드를 강제로 늘리거나 줄이므로, 프리팹과 다르면 목록에서만 카드가 찌그러진다
    /// (팝업과 컷씬은 프리팹 원래 크기를 쓰므로 둘을 같게 맞춰둠)
    /// </summary>
    void MatchGridToPrefab()
    {
        GameObject prefab = CardPrefab;
        var grid = listContent != null ? listContent.GetComponent<GridLayoutGroup>() : null;
        var prt = prefab != null ? prefab.transform as RectTransform : null;
        if (grid == null || prt == null) return;
        Vector2 size = prt.rect.size;
        if (size.x > 1f && size.y > 1f && size != grid.cellSize) grid.cellSize = size;
    }

    /// <summary>
    /// 풀에서 인덱스에 해당하는 UI 를 가져오거나, 없으면 새로 만든다.
    /// 새로 만들 때 한 번만 클릭 이벤트를 붙인다 (RewardCardUI 처럼 인덱스 고정 캡처).
    /// </summary>
    UIViewerCard GetOrCreateCardUI(int index)
    {
        if (index < _cardUIPool.Count) return _cardUIPool[index];

        GameObject prefab = CardPrefab;
        if (prefab == null || listContent == null)
        {
            Debug.LogWarning("[RestSceneController] UI_ViewerCard 프리팹이 비어 있습니다.");
            return null;
        }

        GameObject obj = Instantiate(prefab, listContent);
        UIViewerCard cardUI = obj.GetComponent<UIViewerCard>();
        if (cardUI == null)
        {
            Debug.LogWarning("[RestSceneController] 프리팹에 UIViewerCard 컴포넌트가 없습니다.");
            Destroy(obj);
            return null;
        }

        // 클릭을 받을 그래픽이 루트에 없으면 투명 이미지 추가
        if (obj.GetComponent<Graphic>() == null)
        {
            var hit = obj.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
        }
        else obj.GetComponent<Graphic>().raycastTarget = true;

        // 프리팹에 버튼이 있으면 그대로 쓰고 (호버 효과 유지), 없을 때만 추가
        Button button = obj.GetComponent<Button>();
        if (button == null)
        {
            button = obj.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }

        int idx = index;
        button.onClick.AddListener(() => OnClickCardSelect(idx));

        _cardUIPool.Add(cardUI);
        return cardUI;
    }

    /// <summary>목록의 카드 클릭 시 (idx = 정렬된 목록 기준)</summary>
    void OnClickCardSelect(int idx)
    {
        if (_busy || idx < 0 || idx >= _listCards.Count) return;
        CardInstance card = _listCards[idx];
        if (card == null) return;
        _picked = card;
        HideTooltip();   // 목록 카드에 떠 있던 툴팁이 팝업 위에 남지 않게

        if (_mode == Mode.Upgrade)
        {
            Clear(compareBefore); Clear(compareAfter);
            _pickedView = Place(bridge.CreateCardView(card, compareBefore, false));
            Place(bridge.CreateCardView(card, compareAfter, true));
            comparePopup.SetActive(true);
        }
        else
        {
            Clear(removeSlot);
            _pickedView = Place(bridge.CreateCardView(card, removeSlot, false));
            removePopup.SetActive(true);
        }
    }

    void ConfirmUpgrade()
    {
        if (_busy || _pickedView == null) return;
        _busy = true;
        // 코루틴의 첫 줄에서 카드가 컷씬 캔버스로 옮겨진 뒤 팝업을 닫는다
        StartCoroutine(cutscenes.PlayUpgrade(_pickedView, _picked));
        comparePopup.SetActive(false);
        listClose.gameObject.SetActive(false);
    }

    void ConfirmRemove()
    {
        if (_busy || _pickedView == null) return;
        _busy = true;
        StartCoroutine(cutscenes.PlayRemove(_pickedView, _picked));
        removePopup.SetActive(false);
        listClose.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// 카드 목록 표시용 정렬 (DeckViewerPopup.SortCardsForViewer 와 같은 기준)
    /// 1순위 ConditionCategory, 2순위 CardType, 3순위 RankType, 4순위 CardId
    /// </summary>
    static void SortCardsForViewer(List<CardInstance> cards)
    {
        cards.Sort((a, b) =>
        {
            if (a == null || a.baseData == null) return (b == null || b.baseData == null) ? 0 : 1;
            if (b == null || b.baseData == null) return -1;

            CardSO dataA = a.baseData, dataB = b.baseData;

            int c = ((int)dataA.conditionCategory).CompareTo((int)dataB.conditionCategory);
            if (c != 0) return c;
            c = ((int)dataA.cardType).CompareTo((int)dataB.cardType);
            if (c != 0) return c;
            c = ((int)dataA.rank).CompareTo((int)dataB.rank);
            if (c != 0) return c;
            return dataA.cardId.CompareTo(dataB.cardId);
        });
    }

    /// <summary>UIViewerCard 의 TooltipTrigger 로 떠 있던 툴팁 닫기 (CardDisplay 와 같은 방식)</summary>
    static void HideTooltip()
    {
        if (TooltipPanel.Instance != null) TooltipPanel.Instance.HideTooltip();
    }

    static void Clear(RectTransform slot)
    {
        for (int i = slot.childCount - 1; i >= 0; i--) Destroy(slot.GetChild(i).gameObject);
    }

    static RectTransform Place(RectTransform view)
    {
        if (view == null) return null;
        view.anchorMin = view.anchorMax = new Vector2(0.5f, 0.5f);
        view.anchoredPosition = Vector2.zero;
        view.localScale = Vector3.one;
        // 팝업의 카드는 클릭되지 않게. interactable = false 는 프리팹 버튼의 '비활성' 색(어둡게)이 적용되므로
        // 컴포넌트를 꺼서 원래 색 그대로 둔다
        var b = view.GetComponent<Button>();
        if (b != null) b.enabled = false;
        return view;
    }
}
