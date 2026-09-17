using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabaseSO", menuName = "SO/Database/CardDatabaseSO")]
public class CardDatabaseSO : BaseDatabaseSO<int, CardSO>
{
    protected override int GetKey(CardSO item) => item.cardId;

    // 함수 이름 직관적이게 변경
    public CardSO GetCardById(int id) => GetByKey(id);
}