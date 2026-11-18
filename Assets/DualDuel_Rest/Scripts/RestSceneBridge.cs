using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 휴식 씬과 게임 데이터(체력, 보유 덱)를 잇는 다리.
/// 카드 목록과 카드 UI 는 실제 CardInstance 와 UI_ViewerCard 프리팹(UIViewerCard)으로 다룬다.
/// 컷씬(RestCutscenes)은 카드를 object 로 주고받으므로 그쪽 메서드는 object 시그니처를 유지한다.
/// </summary>
public abstract class RestSceneBridge : MonoBehaviour
{
    [Tooltip("휴식 후 이동할 스테이지 선택 씬 이름 (Build Settings 에 등록돼 있어야 함)")]
    public string stageSelectScene = "StageSelectScene";

    [Tooltip("최대 체력 대비 회복 비율")]
    [Range(0f, 1f)] public float healRatio = 0.2f;

    [Tooltip("카드 표시용 프리팹 (DeckViewerPopup 과 같은 UI_ViewerCard, UIViewerCard 컴포넌트)")]
    public GameObject viewerCardPrefab;

    // ------------------------------------------------------------------ 체력

    public abstract int CurrentHP { get; }
    public abstract int MaxHP { get; }
    public virtual int HealAmount => Mathf.CeilToInt(MaxHP * healRatio);
    public int HPAfterHeal => Mathf.Min(MaxHP, CurrentHP + HealAmount);

    /// <summary>회복 적용 (컷씬의 번쩍임 순간에 호출됨)</summary>
    public abstract void ApplyHeal(int amount);

    // ------------------------------------------------------------------ 덱

    /// <summary>현재 보유 덱 원본. 컨트롤러가 복사본을 만들어 정렬하므로 원본 순서는 바뀌지 않음</summary>
    public abstract List<CardInstance> GetDeck();

    /// <summary>강화 목록에 보일 카드인지 (최대 강화 단계 등)</summary>
    public virtual bool CanUpgrade(CardInstance card) => card != null && card.baseData != null;

    /// <summary>강화 후 모습 미리보기용 임시 CardInstance (원본은 건드리지 않음)</summary>
    public abstract CardInstance MakeUpgradedPreview(CardInstance card);

    /// <summary>강화 적용 (컷씬의 번쩍임 순간에 호출됨). card 는 CardInstance</summary>
    public abstract void ApplyUpgrade(object card);

    /// <summary>덱에서 제거 (컷씬의 폭발 순간에 호출됨). card 는 CardInstance</summary>
    public abstract void RemoveCard(object card);

    // ------------------------------------------------------------------ 카드 UI

    /// <summary>
    /// UI_ViewerCard 프리팹으로 카드 UI 를 만들어 반환. 컨트롤러와 컷씬이 함께 사용.
    /// upgradedPreview = true 면 강화 후 모습으로.
    /// </summary>
    public virtual RectTransform CreateCardView(object card, Transform parent, bool upgradedPreview)
    {
        var instance = card as CardInstance;
        if (instance == null || viewerCardPrefab == null)
        {
            Debug.LogWarning("[RestSceneBridge] 카드가 CardInstance 가 아니거나 viewerCardPrefab 이 비어 있습니다.");
            return null;
        }
        if (upgradedPreview) instance = MakeUpgradedPreview(instance);

        GameObject go = Instantiate(viewerCardPrefab, parent);
        var ui = go.GetComponent<UIViewerCard>();
        if (ui != null) ui.Setup(instance);
        return go.transform as RectTransform;
    }

    /// <summary>
    /// 이미 만든 카드 UI 를 같은 카드의 현재 값으로 갱신 (강화 적용 직후, 컷씬이 호출).
    /// UIViewerCard.RefreshValues 가 강화 단계가 반영된 밸류 / 리워드 텍스트를 다시 그린다.
    /// 갱신했으면 true, 할 수 없으면 false (그때는 컷씬이 카드 UI 를 새로 만든다)
    /// </summary>
    public virtual bool RefreshCardView(RectTransform view, object card)
    {
        var instance = card as CardInstance;
        var ui = view != null ? view.GetComponent<UIViewerCard>() : null;
        if (instance == null || ui == null) return false;
        ui.RefreshValues(instance);
        return true;
    }

    /// <summary>모든 연출이 끝난 뒤. 기본은 글리치 전환으로 스테이지 선택 씬 로드</summary>
    public virtual void LeaveRest()
    {
        // 스테이지 복귀
        if (StageManager.Instance != null)
        {
            StageManager.Instance.ReturnToStageSelect();
        }
    }
}
