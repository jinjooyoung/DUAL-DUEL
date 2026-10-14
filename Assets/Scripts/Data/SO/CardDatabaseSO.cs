using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "CardDatabaseSO", menuName = "SO/Database/CardDatabaseSO")]
public class CardDatabaseSO : BaseDatabaseSO<int, CardSO>, IRankDatabase<RankType, CardSO>
{
    private Dictionary<RankType, List<CardSO>> ListByRank;
    protected override int GetKey(CardSO item) => item.cardId;

    // 함수 이름 직관적이게 변경
    public CardSO GetCardById(int id) => GetByKey(id);

    protected override void OnInitialize()
    {
        ListByRank = new Dictionary<RankType, List<CardSO>>();

        foreach (var item in items)
        {
            if (item == null) continue;

            RankType rank = item.rank;

            if (!ListByRank.TryGetValue(rank, out var rankList))
            {
                rankList = new List<CardSO>();
                ListByRank.Add(rank, rankList);
            }

            rankList.Add(item);
        }
    }

    public IReadOnlyList<CardSO> GetListByRank(RankType rank)
    {
        if (ListByRank == null) Initialize();   // Initialize에서 OnInitialize 호출하니까

        if (ListByRank.TryGetValue(rank, out var item))
            return item;

        return Array.Empty<CardSO>();
    }
}