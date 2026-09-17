using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationDatabaseSO", menuName = "SO/Database/LocalizationDatabaseSO")]
public class LocalizationDatabaseSO : BaseDatabaseSO<string, LocalizationSO>
{
    protected override string GetKey(LocalizationSO item) => item.key;

    public LocalizationSO GetWordById(string key) => GetByKey(key);
}