using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterData
{
    public int characterID;

    public string characterName;
    public string descriptionText;
    public string playStyleText;

    public string startDeckID;
    public int signatureCardID;

    public int maxHp;
    public int startingLife;
}

[CreateAssetMenu(fileName = "CharacterSO", menuName = "SO/DataSO/CharacterSO")]
public class CharacterSO : ScriptableObject
{
    public int characterID;

    public string characterName;
    public string descriptionText;
    public string playStyleText;

    public List<int> startDeckID = new List<int>();
    public int signatureCardID;

    public int maxHp;
    public int startingLife;
}
