using System.Collections.Generic;
using UnityEngine;

public interface IInitializableDatabase
{
    void Initialize();
}

public abstract class BaseDatabaseSO<TKey, TItem> : ScriptableObject, IInitializableDatabase
{
    public List<TItem> items = new List<TItem>();

    private Dictionary<TKey, TItem> itemByKey;

    protected abstract TKey GetKey(TItem item);

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
    }

    public TItem GetByKey(TKey key)
    {
        if (itemByKey == null) Initialize();

        if (itemByKey.TryGetValue(key, out var item))
            return item;

        return default;
    }
}

// -------------------------------------------------------------
// 데이터베이스 클래스들
// -------------------------------------------------------------

[CreateAssetMenu(fileName = "CardDatabaseSO", menuName = "SO/Database/CardDatabaseSO")]
public class CardDatabaseSO : BaseDatabaseSO<int, CardSO>
{
    protected override int GetKey(CardSO item) => item.cardId;

    // 함수 이름 직관적이게 변경
    public CardSO GetCardById(int id) => GetByKey(id);
}

[CreateAssetMenu(fileName = "LocalizationDatabaseSO", menuName = "SO/Database/LocalizationDatabaseSO")]
public class LocalizationDatabaseSO : BaseDatabaseSO<string, LocalizationSO>
{
    protected override string GetKey(LocalizationSO item) => item.key;

    public LocalizationSO GetWordById(string key) => GetByKey(key);
}