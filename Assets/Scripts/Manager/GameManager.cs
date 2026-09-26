using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전역 상태 관리하는 싱글턴 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("플레이어 영구 보유 덱")]
    public List<CardInstance> deck = new List<CardInstance>();
    [Header("현재 캐릭터")]
    public CharacterSO character;
    [Header("현재 몬스터")]
    public MonsterSO monster;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
