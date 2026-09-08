using System.Collections.Generic;
using UnityEngine;

public enum OwnerType
{
    Player,
    Enemy
}

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

[CreateAssetMenu(fileName = "CardSO", menuName = "SO/DataSO/CardSO")]
public class CardSO : ScriptableObject
{
    public int cardId;

    public OwnerType ownerType;
    public CardType cardType;
    public RankType rank;           // 데이터테이블은 숫자로 작성하고 SO에는 타입으로 불러옴

    public Sprite artwork;          // 스프라이트는 SO 생성 에디터에서 리소스폴더에 있는 Card_XXXX 아이디로 찾아와서 할당하도록 할 예정

    public string nameKey;
    public string descKey;

    public List<int> values = new List<int>();
}
