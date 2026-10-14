using System;
using System.Collections.Generic;

/// <summary>
/// 등급(TRank)별 데이터 캐싱 및 조회를 지원하는 범용 중간 데이터베이스 클래스
/// </summary>
public abstract class BaseRankDatabaseSO<TKey, TRank, TItem> : BaseDatabaseSO<TKey, TItem>, IRankDatabase<TRank, TItem>
{
    private Dictionary<TRank, List<TItem>> listByRank;

    /// <summary>
    /// 아이템에서 등급(Rank) 키를 추출하는 추상 메서드 (자식 클래스에서 정의)
    /// </summary>
    protected abstract TRank GetRank(TItem item);

    protected override void OnInitialize()
    {
        base.OnInitialize();

        listByRank = new Dictionary<TRank, List<TItem>>();

        foreach (var item in items)
        {
            if (item == null) continue;

            TRank rank = GetRank(item);

            if (!listByRank.TryGetValue(rank, out var rankList))
            {
                rankList = new List<TItem>();
                listByRank.Add(rank, rankList);
            }

            rankList.Add(item);
        }
    }

    /// <summary>
    /// 등급에 해당하는 아이템 리스트 반환 (미등록 등급일 경우 빈 컬렉션 반환)
    /// </summary>
    public IReadOnlyList<TItem> GetListByRank(TRank rank)
    {
        if (listByRank == null) Initialize();

        if (listByRank.TryGetValue(rank, out var list))
            return list;

        return Array.Empty<TItem>();
    }
}