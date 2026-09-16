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

[Serializable]
public class CardData
{
    public int cardId;
    public string cardType;     // 카드의 타입 (공격, 방어, 버프 등)
    public int isSpecial;       // 특수 카드인지 아닌지
    public int rank;            // 1~6까지의 등급을 나타냄. 높을수록 희귀(좋은) 등급

    public string nameKey;      // 추후 로컬라이징 적용할 때 사용할 카드 이름 키
    public string descKey;      // 설명 키

    public int baseValue;       // 기본 수치
    public int upgrade_1;       // 업그레이드 1회 수치
    public int upgrade_2;       // 업그레이드 2회 수치
    public int upgrade_3;       // 업그레이드 3회 수치
    public int upgrade_4;       // 업그레이드 4회 수치
    public int upgrade_5;       // 업그레이드 5회 수치

    // 특수 카드 개별 밸류
    public int playerSlotValue;
    public int enemySlotValue;
}


[CreateAssetMenu(fileName = "CardSO", menuName = "SO/DataSO/CardSO")]
public class CardSO : ScriptableObject
{
    public int cardId;

    public CardType cardType;
    public bool isSpecial;          // 스페셜 타입을 따로 만들려고 했는데 공격, 방어 등 기본 타입이 어느건지는 알 수 있어야할 것 같아서 bool 값으로 뺌
    public RankType rank;           // 데이터테이블은 숫자로 작성하고 SO에는 타입으로 불러옴

    public Sprite artwork;          // 스프라이트는 SO 생성 에디터에서 리소스폴더에 있는 Card_XXXX 아이디로 찾아와서 할당하도록 할 예정

    public string nameKey;
    public string descKey;

    public List<int> values = new List<int>();

    // 특수 카드 일 경우 (일반 5타입 카드는 둘다 0)
    // 플레이어 슬롯에 꽂혔을 때의 수치 vs 적 슬롯에 꽂혔을 때의 수치
    public int playerSlotValue; // 예: 자해 -4 또는 실드 2
    public int enemySlotValue;  // 예: 적의 공격력 12
}
