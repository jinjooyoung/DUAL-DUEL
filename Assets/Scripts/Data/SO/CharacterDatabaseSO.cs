using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabaseSO", menuName = "SO/Database/CharacterDatabaseSO")]
public class CharacterDatabaseSO : BaseDatabaseSO<int, CharacterSO>
{
    protected override int GetKey(CharacterSO item) => item.characterID;

    public CharacterSO GetWordById(int id) => GetByKey(id);
}