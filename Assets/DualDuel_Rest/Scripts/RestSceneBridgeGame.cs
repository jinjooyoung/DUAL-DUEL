using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제 게임 데이터용 휴식 씬 브리지.
///   덱       GameManager.Instance.deck (List<CardInstance>)
///   체력     GameManager.Instance.currentHP / GameManager.Instance.character.maxHp
///   강화     CardInstance.upgradeLevel++ (수치는 항상 values[upgradeLevel] 로 읽으므로 레벨만 올리면 끝)
///   제거     GameManager.Instance.deck 에서 Remove
/// </summary>
public class RestSceneBridgeGame : RestSceneBridge
{
    static GameManager GM => GameManager.Instance;

    // ------------------------------------------------------------------ 체력

    public override int CurrentHP => GM != null ? GM.currentHP : 0;
    public override int MaxHP => GM != null && GM.character != null ? GM.character.maxHp : 0;

    public override void ApplyHeal(int amount)
    {
        if (GM == null) { Debug.LogWarning("[RestSceneBridgeGame] GameManager.Instance 가 없어 회복을 적용하지 못했습니다."); return; }
        GM.currentHP = Mathf.Min(MaxHP, GM.currentHP + amount);
    }

    // ------------------------------------------------------------------ 덱

    public override List<CardInstance> GetDeck()
    {
        if (GM == null)
        {
            Debug.LogWarning("[RestSceneBridgeGame] GameManager.Instance 가 없습니다. 휴식 씬을 단독으로 실행했다면 덱이 비어 보입니다.");
            return new List<CardInstance>();
        }
        return GM.deck;
    }

    public override void RemoveCard(object card)
    {
        var instance = card as CardInstance;
        if (instance == null || GM == null) return;
        if (!GM.deck.Remove(instance)) Debug.LogWarning("[RestSceneBridgeGame] 덱에서 카드를 찾지 못해 제거하지 못했습니다.");
    }

    // ------------------------------------------------------------------ 강화

    /// <summary>강화 목록에는 더 강화할 수 있는 카드만</summary>
    public override bool CanUpgrade(CardInstance card) => card != null && card.CanUpgrade;

    /// <summary>
    /// 강화 후 미리보기: 한 단계 위의 임시 CardInstance (덱에는 들어가지 않음).
    /// 생성자가 최대 강화 단계로 보정하므로 범위를 넘지 않는다.
    /// </summary>
    public override CardInstance MakeUpgradedPreview(CardInstance card)
    {
        if (card == null || card.baseData == null) return card;
        return new CardInstance(card.baseData, card.upgradeLevel + 1);
    }

    /// <summary>강화 적용 (컷씬의 번쩍임 순간에 호출됨)</summary>
    public override void ApplyUpgrade(object card)
    {
        if (card is CardInstance instance) CardEnhance(instance);
    }

    public void CardEnhance(CardInstance card)
    {
        // 강화 불가면 리턴
        if (card == null || !card.CanUpgrade) return;

        // 강화 완료 시 업그레이드 레벨 증가 후 마무리
        card.upgradeLevel++;
    }
}
