using System;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Attack,
    Defense,
    Heal,
    Buff,
    Debuff
}

public enum RankType
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5,
    Mythic = 6
}

// 추가 효과 조건 타입
public enum ConditionCategory
{
    None = 0,
    SlotIndexRestriction = 1,   // 슬롯 번호 검사 (예: 0, 4, "0,4")
    NeighborCardType = 2,       // 인접(앞/뒤) 슬롯의 카드 타입 검사 (예: Attack)
    PrevSlotOwner = 3,          // 직전 슬롯 소유자 검사 (Player, Enemy)
    HandCount = 4,              // 손패 카드 장수 검사 (예: "Count>=5")
    HandType = 5                // 손패 특정 타입 매수 검사 (예: "Attack>=4")
}

// 조건 달성 시 보상 타입
public enum RewardCategory
{
    None = 0,
    FlatBonus = 1,              // 정수 밸류 가산 (+N)
    Multiplier = 2,             // 밸류 곱연산 (*N)
    RepeatCast = 3,             // 해당 카드 N회 다중 시전 (연타)
    TriggerNeighborSlot = 4,    // 인접 슬롯 카드 추가 발동 (0: 양쪽, -1: 앞, 1: 뒤)
    ModifyNextDraw = 5,         // 다음 턴 드로우 매수 조정 (+N장)
    ApplyBuffDebuff = 6         // 버프/디버프 부여
}

[Serializable]
public class CardData
{
    public int cardId;
    public string cardType;     // 카드의 타입 (공격, 방어, 버프 등)
    public int rank;            // 1~6까지의 등급을 나타냄. 높을수록 희귀(좋은) 등급

    public string nameKey;      // 추후 로컬라이징 적용할 때 사용할 카드 이름 키
    public string descKey;      // 설명 키

    public int baseValue;       // 기본 수치
    public int upgrade_1;       // 업그레이드 1회 수치
    public int upgrade_2;       // 업그레이드 2회 수치
    public int upgrade_3;       // 업그레이드 3회 수치
    public int upgrade_4;       // 업그레이드 4회 수치
    public int upgrade_5;       // 업그레이드 5회 수치

    public bool isExhaust;      // 소멸 여부 (해당 전투에서 1회만 사용 가능한 카드인지)

    public string conditionCategory;    // 추가 효과 조건 타입
    public string conditionParam;       // 조건 수치

    public string rewardCategory;       // 조건 달성 시 보상 타입
    public float rewardParam;           // 보상 수치
}


[CreateAssetMenu(fileName = "CardSO", menuName = "SO/DataSO/CardSO")]
public class CardSO : ScriptableObject
{
    [Header("기본 정보")]
    public int cardId;
    public CardType cardType;
    public RankType rank;           // 데이터테이블은 숫자로 작성하고 SO에는 타입으로 불러옴
    public Sprite artwork;          // 스프라이트는 SO 생성 에디터에서 리소스폴더에 있는 Card_XXXX 아이디로 찾아와서 할당하도록 할 예정

    [Header("로컬라이징 키")]
    public string nameKey;
    public string descKey;

    [Header("성장 수치 (Index 0: 기본, 1~5: 강화 단계)")]
    public List<int> values = new List<int>();

    [Header("소멸 여부")]
    public bool isExhaust;

    [Header("모듈형 기믹 조건")]
    public ConditionCategory conditionCategory;
    public string conditionParam;

    [Header("조건 달성 보상")]
    public RewardCategory rewardCategory;
    public float rewardParam;
}
