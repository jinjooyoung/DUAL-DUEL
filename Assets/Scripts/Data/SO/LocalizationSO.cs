using System;
using UnityEngine;

public enum LanguageType
{
    KO,
    EN,
    JP
}

[Serializable]
public class LocalizationData
{
    public string key;
    public string ko;
    public string en;
    public string jp;
}

[CreateAssetMenu(fileName = "LocalizationSO", menuName = "SO/DataSO/LocalizationSO")]
public class LocalizationSO : ScriptableObject
{
    public string key;
    public string ko;
    public string en;
    public string jp;
}
