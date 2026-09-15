using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationDatabaseSO", menuName = "SO/Database/LocalizationDatabaseSO")]
public class LocalizationDatabaseSO : ScriptableObject
{
    public List<LocalizationSO> words = new List<LocalizationSO>();

    // Ä³½Ì¿ë µñ¼Å³Ê¸®
    private Dictionary<string, LocalizationSO> wordByKey;

    public void Initialize()
    {
        wordByKey = new Dictionary<string, LocalizationSO>();

        foreach (var word in words)
        {
            wordByKey[word.key] = word;
        }
    }

    // key·Î ¹ø¿ª Ã£±â
    public LocalizationSO GetWordById(string key)
    {
        if (wordByKey == null)
        {
            Initialize();
        }

        if (wordByKey.TryGetValue(key, out LocalizationSO word))
            return word;

        return null;
    }
}
