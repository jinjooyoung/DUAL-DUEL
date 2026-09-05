using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabaseSO", menuName = "SO/Database/CardDatabaseSO")]
public class CardDatabaseSO : ScriptableObject
{
    public List<CardSO> cards = new List<CardSO>();

    // Ä³½Ì¿ë µñ¼Å³Ê¸®
    private Dictionary<int, CardSO> cardById;

    public void Initialize()
    {
        cardById = new Dictionary<int, CardSO>();

        foreach (var card in cards)
        {
            cardById[card.cardId] = card;
        }
    }

    // ID·Î Ä«µå Ã£±â
    public CardSO GetCardById(int id)
    {
        if (cardById == null)
        {
            Initialize();
        }

        if (cardById.TryGetValue(id, out CardSO card))
            return card;

        return null;
    }
}
