using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전역 상태 관리하는 싱글턴 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("플레이어 영구 보유 덱")]
    public List<CardInstance> deck = new List<CardInstance>();  // 런타임용

    // 게임 오버 시 다시 불러오는 용
    [SerializeField] private List<CardInstance> startDeck = new List<CardInstance>();

    [Header("현재 캐릭터")]
    public CharacterSO character;
    [Header("현재 몬스터")]
    public MonsterSO monster;

    [Header("런타임 데이터")]
    public int currentHP;
    public int currentGold;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            foreach (var card in deck)
            {
                startDeck.Add(card);
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        currentHP = character.maxHp;
    }

    public void DeckInit()
    {
        deck.Clear();

        foreach (var card in GameManager.Instance.startDeck)
        {
            deck.Add(card);
        }
    }

    public void StatInit()
    {
        character = DataManager.Instance.GetCharacter(1001);
        currentHP = character.maxHp;

        currentGold = 0;
    }
}
