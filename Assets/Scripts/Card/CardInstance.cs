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
    /// 더 강화할 수 있는지 (강화 로직 구현 시 사용)
    /// </summary>
    public bool CanUpgrade => baseData != null && baseData.CanUpgrade(upgradeLevel);
}
