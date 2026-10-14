using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabaseSO", menuName = "SO/Database/CardDatabaseSO")]
public class CardDatabaseSO : BaseRankDatabaseSO<int, RankType, CardSO>
{
    protected override int GetKey(CardSO item) => item.cardId;

    protected override RankType GetRank(CardSO item) => item.rank;

    public CardSO GetCardById(int id) => GetByKey(id);
}