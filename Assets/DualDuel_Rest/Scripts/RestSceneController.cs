using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 휴식 씬 화면 흐름.
///   선택 화면 (회복 / 강화 / 제거)
///   회복 -> 팝업 (회복하기 / 돌아가기) -> 회복 컷씬
///   강화 -> 카드 목록 -> 카드 클릭 -> 강화 전/후 비교 (강화하기 / 돌아가기) -> 강화 컷씬
///   제거 -> 카드 목록 -> 카드 클릭 -> 카드 1장 (제거하기 / 돌아가기) -> 제거 컷씬
/// 모든 참조는 Tools > DUAL DUEL > 휴식 씬 구성 툴이 채운다.
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
    public RectTransform listContent;
    public ScrollRect listScroll;
    public Button listClose;

    [Header("강화 비교")]
    public GameObject comparePopup;
    public RectTransform compareBefore, compareAfter;
    public Button upgradeConfirm, upgradeCancel;

    [Header("제거 확인")]
    public GameObject removePopup;
    public RectTransform removeSlot;
    public Button removeConfirm, removeCancel;

    Mode _mode;
    object _picked;
    RectTransform _pickedView;
    bool _busy;

    void Start()
    {
        healOption.onClick.AddListener(OpenHeal);
        upgradeOption.onClick.AddListener(() => OpenList(Mode.Upgrade));
        removeOption.onClick.AddListener(() => OpenList(Mode.Remove));
        healConfirm.onClick.AddListener(ConfirmHeal);
        healCancel.onClick.AddListener(() => healPopup.SetActive(false));
        listClose.onClick.AddListener(() => listOverlay.SetActive(false));
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
        listTitle.text = mode == Mode.Upgrade ? "강화할 카드를 선택하세요" : "제거할 카드를 선택하세요";
        for (int i = listContent.childCount - 1; i >= 0; i--) Destroy(listContent.GetChild(i).gameObject);

        IList<object> cards = bridge.GetCards(mode == Mode.Upgrade);
        foreach (object card in cards)
        {
            RectTransform view = bridge.CreateCardView(card, listContent, false);
            if (view == null) continue;
            var button = view.GetComponent<Button>();
            if (button == null) button = view.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            object c = card;
            button.onClick.AddListener(() => PickCard(c));
            var g = view.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;
        }
        listOverlay.SetActive(true);
        if (listScroll != null) listScroll.verticalNormalizedPosition = 1f;
    }

    void PickCard(object card)
    {
        if (_busy) return;
        _picked = card;
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
        var b = view.GetComponent<Button>();
        if (b != null) b.interactable = false;
        return view;
    }
}
