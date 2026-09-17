using System;
using System.Collections.Generic;
using UnityEngine;

public enum GimmickType
{
    None,
    Spike,
    LockSlot,
    Hacking
}

[Serializable]
public class MonsterData
{
    public int monsterID;

    public string monsterName;

    public int maxHp;

    public int slotCreateType;

    public string monsterDeckID;
    public int rewardCardRank;

    public string gimmickType;
    public int gimmickValue;
}

[CreateAssetMenu(fileName = "MonsterSO", menuName = "SO/DataSO/MonsterSO")]
public class MonsterSO : ScriptableObject
{
    public int monsterID;

    public string monsterName;

    public int maxHp;

    public int slotCreateType;

    public List<int> monsterDeckID = new List<int>();
    public int rewardCardRank;

    public GimmickType gimmickType;
    public int gimmickValue;
}
