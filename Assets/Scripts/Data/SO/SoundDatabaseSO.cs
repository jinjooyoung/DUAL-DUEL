using System;
using System.Collections.Generic;
using UnityEngine;

public enum BGMType
{
    None = 0,
    Title,
    StageSelect,
    Battle_Normal,
    Battle_Boss,
    Shop,
    Rest,
    Victory,
    Defeat
}

public enum SFXType
{
    ButtonClick = 0,
    CardDraw,
    CardPlace,
    CardPlay,
    Attack_Slash,
    Shield_Taking,
    Shield_Block,
    Hit_Damage,
    Heal,
    Buff
}

[Serializable]
public class SoundData<T>
{
    public T soundType;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1.0f;
    [Range(0.5f, 1.5f)] public float pitch = 1.0f;
}

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "SO/Database/SoundDatabaseSO")]
public class SoundDatabaseSO : ScriptableObject
{
    [Header("BGM 사운드 목록")]
    public List<SoundData<BGMType>> bgmList = new List<SoundData<BGMType>>();

    [Header("SFX 효과음 목록")]
    public List<SoundData<SFXType>> sfxList = new List<SoundData<SFXType>>();

    private Dictionary<BGMType, SoundData<BGMType>> bgmDict;
    private Dictionary<SFXType, SoundData<SFXType>> sfxDict;

    /// <summary>
    /// SoundManager에서 호출하여 딕셔너리로 빠른 조회를 준비합니다.
    /// </summary>
    public void Initialize()
    {
        bgmDict = new Dictionary<BGMType, SoundData<BGMType>>();
        foreach (var item in bgmList)
        {
            if (item.clip != null && !bgmDict.ContainsKey(item.soundType))
            {
                bgmDict.Add(item.soundType, item);
            }
        }

        sfxDict = new Dictionary<SFXType, SoundData<SFXType>>();
        foreach (var item in sfxList)
        {
            if (item.clip != null && !sfxDict.ContainsKey(item.soundType))
            {
                sfxDict.Add(item.soundType, item);
            }
        }
    }

    public SoundData<BGMType> GetBGM(BGMType type)
    {
        if (bgmDict == null) Initialize();
        bgmDict.TryGetValue(type, out var data);
        return data;
    }

    public SoundData<SFXType> GetSFX(SFXType type)
    {
        if (sfxDict == null) Initialize();
        sfxDict.TryGetValue(type, out var data);
        return data;
    }
}