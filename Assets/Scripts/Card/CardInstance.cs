using UnityEngine;

[System.Serializable]
public class CardInstance   // SO는 정적 데이터. 이건 런타임용
{
    public string instanceId;       // 고유 ID (인스턴스 구별용 Guid)
    public CardSO baseData;         // 원본 정적 SO 참조
    public int upgradeLevel = 0;    // 현재 강화 단계 (0 = 기본, 1 = +1강, 2 = +2강)

    // 생성자
    public CardInstance(CardSO data, int level = 0)
    {
        this.instanceId = System.Guid.NewGuid().ToString();
        this.baseData = data;

        // 카드별 최대 강화 횟수를 넘는 단계로 생성되지 않도록 보정
        int maxLevel = data != null ? data.maxUpgradeCount : 0;
        this.upgradeLevel = Mathf.Clamp(level, 0, maxLevel);
    }

    /// <summary>
    /// 현재 강화 단계에 맞는 기본 밸류를 반환합니다. (범위 보정은 CardSO에서 처리)
    /// </summary>
    public int GetValue()
    {
        return baseData != null ? baseData.GetValue(upgradeLevel) : 0;
    }

    /// <summary>
    /// 현재 강화 단계에 맞는 리워드 파람을 반환합니다.
    /// upgradeTarget이 0(밸류 강화)인 카드는 단계와 상관없이 0강 값과 같습니다.
    /// </summary>
    public float GetRewardParam()
    {
        return baseData != null ? baseData.GetRewardParam(upgradeLevel) : 0f;
    }

    /// <summary>
    /// 기믹 아이콘 옆에 표시할 보상 텍스트 (3D 카드 / UI 카드 공용)
    /// </summary>
    public string GetRewardDisplayText()
    {
        if (baseData == null) return string.Empty;

        float rewardParam = GetRewardParam();

        switch (baseData.rewardCategory)
        {
            case RewardCategory.None:                return string.Empty;
            case RewardCategory.FlatBonus:           return $"+{rewardParam}";   // 예: +4
            case RewardCategory.Multiplier:          return $"x{rewardParam}";   // 예: x2
            case RewardCategory.RepeatCast:          return $"+{rewardParam}";   // 예: +2 (추가 시전 횟수)
            case RewardCategory.TriggerNeighborSlot: return "+1";                // 인접 카드 1회 추가 시전 (파람은 방향값)
            case RewardCategory.ModifyNextDraw:      return $"+{rewardParam}";   // 예: +1 (드로우 증가)
            default:                                 return rewardParam > 0 ? ((int)rewardParam).ToString() : string.Empty;
        }
    }

    /// <summary>
    /// 더 강화할 수 있는지 (강화 로직 구현 시 사용)
    /// </summary>
    public bool CanUpgrade => baseData != null && baseData.CanUpgrade(upgradeLevel);
}
