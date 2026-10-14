using System.Collections.Generic;
using UnityEngine;

public interface IInitializableDatabase
{
    void Initialize();
}

// 등급별 리스트를 가져옴
public interface IRankDatabase<TRank, TItem>
{
    IReadOnlyList<TItem> GetListByRank(TRank rank);
}

public abstract class BaseDatabaseSO<TKey, TItem> : ScriptableObject, IInitializableDatabase
{
    public List<TItem> items = new List<TItem>();

    private Dictionary<TKey, TItem> itemByKey;

    protected abstract TKey GetKey(TItem item);

    // 자식 클래스에 초기화 로직이 필요하다면 오버라이드
    protected virtual void OnInitialize()
    {
    }

    public void Initialize()
    {
        itemByKey = new Dictionary<TKey, TItem>();

        foreach (var item in items)
        {
            if (item == null) continue;

            TKey key = GetKey(item);
            if (!itemByKey.ContainsKey(key))
            {
                itemByKey.Add(key, item);
            }
            else
            {
                Debug.LogWarning($"[{name}] 중복된 키가 발견되었습니다: {key}");
            }
        }

        OnInitialize();
    }

    public TItem GetByKey(TKey key)
    {
        if (itemByKey == null) Initialize();

        if (itemByKey.TryGetValue(key, out var item))
            return item;

        return default;
    }
}