using UnityEngine;

[System.Serializable]
public class CardInstance   // SO는 정적 데이터. 이건 런타임용
{
    public string instanceId;       // 고유 ID (인스턴스 구별용 Guid)
    public CardSO baseData;         // 원본 정적 SO 참조
    public int upgradeLevel = 0;    // 현재 강화 단계 (0 = 기본, 1 = +1강 ...)

    // 생성자
    public CardInstance(CardSO data, int level = 0)
    {
        this.instanceId = System.Guid.NewGuid().ToString();
        this.baseData = data;
        this.upgradeLevel = level;
    }

    /// <summary>
    /// 현재 강화 단계에 맞는 수치를 반환합니다.
    /// </summary>
    public int GetValue()
    {
        if (baseData == null || baseData.values == null || baseData.values.Count == 0) return 0;

        // 강화 단계가 values 리스트 범위를 넘지 않도록 Clamp
        int safeIndex = Mathf.Clamp(upgradeLevel, 0, baseData.values.Count - 1);
        return baseData.values[safeIndex];
    }
}