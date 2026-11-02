using System;
using System.Collections.Generic;
using UnityEngine;

// 정수값이 에셋에 저장되므로 순서를 바꾸거나 중간에 끼워넣지 말 것 (새 타입은 맨 뒤에 추가)
public enum CardType
{
    Attack = 0,
    Defense = 1,
    Heal = 2,

    // 버프 (시전자 본인에게 부여)
    Buff_Regeneration = 3,  // 재생 : 턴 종료 시 체력 +n
    Buff_Force = 4,         // 힘 : 공격 피해량 +n
    Buff_Lifesteal = 5,     // 흡혈 : 체력 피해의 n% 회복 (최대 100)

    // 디버프 (시전자의 상대에게 부여)
    Debuff_Poison = 6,      // 중독 : 턴 종료 시 피해 n
    Debuff_Bleed = 7,       // 출혈 : 공격받을 때 피해 +n
    Debuff_Weaken = 8       // 약화 : 카드 밸류 -n
}

// 일반, 고급, 희귀, 에픽, 전설 (Mythic 삭제)
public enum RankType
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5
}

// 추가 효과 조건 타입
// conditionParam 포맷
//  - NeighborCard / PrevCard / NextCard : "슬롯주인,카드타입"  (슬롯주인: Enemy, Player, Any / 카드타입: CardType 이름 또는 Any)
//  - SlotIndex                          : "0" 또는 "0,4"
public enum ConditionCategory
{
    None = 0,
    NeighborCard = 1,   // 양옆 슬롯 2개의 슬롯 주인 + 배치된 카드 타입 검사 (예: "Enemy,Attack")
    PrevCard = 2,       // 이전 슬롯의 슬롯 주인 + 배치된 카드 타입 검사 (예: "Enemy,Attack")
    NextCard = 3,       // 다음 슬롯의 슬롯 주인 + 배치된 카드 타입 검사 (예: "Enemy,Attack")
    SlotIndex = 4       // 현재 카드가 배치된 슬롯 인덱스 검사 (예: "0", "0,4")
}

// 조건 달성 시 보상 타입
public enum RewardCategory
{
    None = 0,
    FlatBonus = 1,              // 정수 밸류 가산 (+N)
    Multiplier = 2,             // 밸류 곱연산 (*N)
    RepeatCast = 3,             // 해당 카드 N회 추가 시전 (연타)
    TriggerNeighborSlot = 4,    // 인접 슬롯 카드 추가 발동 (코드 기준 0: 이전 슬롯, 그 외: 다음 슬롯)
    ModifyNextDraw = 5,         // 다음 턴 드로우 매수 조정 (+N장)
}

/// <summary>
/// 엑셀 한 행을 그대로 받는 중간 데이터 (열 순서 = 엑셀 Card 시트 열 순서)
/// </summary>
[Serializable]
public class CardData
{
    public int cardId;
    public string cardType;         // 카드 타입 (Attack, Defense, Heal, Buff_Regeneration ... Debuff_Weaken)
    public int rank;                // 1~5 (일반 ~ 전설)

    public string nameKey;
    public string descKey;

    public int baseValue;           // 기본 밸류 (0강)
    public float upgrade_1;         // 1강 시 강화 대상 값 (upgradeTarget 0: 밸류 / 1: 리워드 파람)
    public float upgrade_2;         // 2강 시 강화 대상 값

    public int upgradeTarget;       // 0: 기본 밸류 강화 카드, 1: 리워드 파람 강화 카드
    public int maxUpgradeCount;     // 최대 강화 가능 횟수 (0~2)

    public bool isExhaust;

    public string conditionCategory;
    public string conditionParam;

    public string rewardCategory;
    public float rewardParam;       // 리워드 파람 (0강)
}


[CreateAssetMenu(fileName = "CardSO", menuName = "SO/DataSO/CardSO")]
public class CardSO : ScriptableObject
{
    // 강화 대상 구분값 (enum 대신 정수로 구분)
    public const int UPGRADE_TARGET_VALUE = 0;     // 기본 밸류 강화
    public const int UPGRADE_TARGET_REWARD = 1;    // 리워드 파람 강화
    public const int MAX_UPGRADE_LIMIT = 2;        // 시스템 최대 강화 단계

    [Header("기본 정보")]
    public int cardId;
    public CardType cardType;
    public RankType rank;
    public Sprite artwork;

    public Sprite bgSprite;
    public Sprite typeIconSprite;
    public Sprite gimmikIconSprite;
    public Sprite FilledStarSprite;
    public Sprite EmptyStartSprite;

    [Header("로컬라이징 키")]
    public string nameKey;
    public string descKey;

    [Header("강화 설정")]
    [Tooltip("0: 기본 밸류 강화 카드, 1: 리워드 파람 강화 카드")]
    [Range(0, 1)] public int upgradeTarget;
    [Tooltip("최대 강화 가능 횟수")]
    [Range(0, MAX_UPGRADE_LIMIT)] public int maxUpgradeCount;

    [Header("강화 단계별 수치 (Index = 강화 단계, 길이 = maxUpgradeCount + 1)")]
    public List<int> values = new List<int>();          // 단계별 기본 밸류
    public List<float> rewardParams = new List<float>(); // 단계별 리워드 파람

    [Header("소멸 여부")]
    public bool isExhaust;

    [Header("모듈형 기믹 조건")]
    public ConditionCategory conditionCategory;
    public string conditionParam;

    [Header("조건 달성 보상")]
    public RewardCategory rewardCategory;

    /// <summary>
    /// 강화 단계에 맞는 기본 밸류 (최대 강화 단계를 넘으면 마지막 값으로 고정)
    /// </summary>
    public int GetValue(int upgradeLevel)
    {
        if (values == null || values.Count == 0) return 0;
        return values[Mathf.Clamp(upgradeLevel, 0, values.Count - 1)];
    }

    /// <summary>
    /// 강화 단계에 맞는 리워드 파람
    /// </summary>
    public float GetRewardParam(int upgradeLevel)
    {
        if (rewardParams == null || rewardParams.Count == 0) return 0f;
        return rewardParams[Mathf.Clamp(upgradeLevel, 0, rewardParams.Count - 1)];
    }

    public bool CanUpgrade(int currentLevel) => currentLevel < maxUpgradeCount;

    /// <summary>
    /// 엑셀 CardData -> SO 값 세팅 (스프라이트 할당은 에디터 스크립트에서 별도로)
    /// 강화 대상이 아닌 쪽 리스트는 0강 값으로 채워서, 런타임은 분기 없이 [단계]로만 읽으면 되도록 함
    /// </summary>
    public void SetupFromData(CardData data)
    {
        cardId = data.cardId;
        nameKey = data.nameKey;
        descKey = data.descKey;
        isExhaust = data.isExhaust;
        conditionParam = data.conditionParam;

        cardType = ParseEnum(data.cardType, CardType.Attack, "cardType");
        conditionCategory = ParseEnum(data.conditionCategory, ConditionCategory.None, "conditionCategory");
        rewardCategory = ParseEnum(data.rewardCategory, RewardCategory.None, "rewardCategory");

        if (!Enum.IsDefined(typeof(RankType), data.rank))
            Debug.LogWarning($"[CardSO] {data.cardId}번 카드 rank {data.rank} 는 범위(1~5) 밖이라 보정합니다.");
        rank = (RankType)Mathf.Clamp(data.rank, (int)RankType.Common, (int)RankType.Legendary);

        upgradeTarget = data.upgradeTarget == UPGRADE_TARGET_REWARD ? UPGRADE_TARGET_REWARD : UPGRADE_TARGET_VALUE;
        maxUpgradeCount = Mathf.Clamp(data.maxUpgradeCount, 0, MAX_UPGRADE_LIMIT);

        float[] upgradeColumns = { data.upgrade_1, data.upgrade_2 };

        values.Clear();
        rewardParams.Clear();

        for (int level = 0; level <= maxUpgradeCount; level++)
        {
            if (level == 0)
            {
                values.Add(data.baseValue);
                rewardParams.Add(data.rewardParam);
            }
            else if (upgradeTarget == UPGRADE_TARGET_VALUE)
            {
                values.Add(Mathf.RoundToInt(upgradeColumns[level - 1]));
                rewardParams.Add(data.rewardParam);
            }
            else // UPGRADE_TARGET_REWARD
            {
                values.Add(data.baseValue);
                rewardParams.Add(upgradeColumns[level - 1]);
            }
        }
    }

    private T ParseEnum<T>(string raw, T fallback, string columnName) where T : struct
    {
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw.Trim(), true, out T result) && Enum.IsDefined(typeof(T), result))
            return result;

        if (!string.IsNullOrWhiteSpace(raw))
            Debug.LogWarning($"[CardSO] {cardId}번 카드 {columnName} '{raw}' 를 {typeof(T).Name}으로 변환할 수 없어 {fallback} 로 설정합니다.");
        return fallback;
    }
}
