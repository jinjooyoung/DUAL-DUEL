using UnityEngine;

[CreateAssetMenu(fileName = "MonsterDatabaseSO", menuName = "SO/Database/MonsterDatabaseSO")]
public class MonsterDatabaseSO : BaseDatabaseSO<int, MonsterSO>
{
    protected override int GetKey(MonsterSO item) => item.monsterID;

    public MonsterSO GetMonsterById(int id) => GetByKey(id);
}