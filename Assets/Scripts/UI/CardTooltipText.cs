/// <summary>
/// 카드 설명(툴팁) 문자열 생성. 3D 카드(TooltipTrigger)와 UI 뷰어 카드가 같이 사용
/// 테이블 문자열 규칙
///  - typeDescKey    : {0} = 카드 밸류 (강화 단계 반영)       예) <size=20>공격</size>적에게 {0}만큼의 공격을 가합니다.
///  - gimmickDescKey : {0} = 리워드 파람 (강화 단계 반영)    예) <size=20>호위</size>이전 카드가 아군의 카드라면 {0}번 더 시전합니다.
/// </summary>
public static class CardTooltipText
{
    public const string DEFAULT_TABLE = "Card_Table";

    public static string Build(CardInstance card, string tableName = DEFAULT_TABLE)
    {
        if (card == null || card.baseData == null) return string.Empty;

        CardSO data = card.baseData;

        string typeDesc = LocalizedTextUtil.Get(tableName, data.typeDescKey, null, card.GetValue());

        // 보상이 없는 카드는 기믹 설명 생략
        string gimmickDesc = data.rewardCategory != RewardCategory.None
            ? LocalizedTextUtil.Get(tableName, data.gimmickDescKey, null, card.GetRewardParam())
            : string.Empty;

        if (string.IsNullOrEmpty(gimmickDesc)) return typeDesc;
        if (string.IsNullOrEmpty(typeDesc)) return gimmickDesc;
        return $"{typeDesc}\n{gimmickDesc}";
    }
}
