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

    // 시작 덱 (강화 0, 런타임 중에 바꾸지 않는 참조용). 저장이 없을 때 / 죽어서 다시 시작할 때 덱을 이걸로 초기화
    // readonly 를 붙이면 유니티가 직렬화하지 않아 인스펙터 값이 저장되지 않음 (항상 빈 덱이 됨) -> private 으로 막고 읽기 전용으로만 공개
    [SerializeField] private List<CardInstance> startDeck = new List<CardInstance>();

    /// <summary>
    /// 시작 덱 (읽기 전용)
    /// </summary>
    public IReadOnlyList<CardInstance> StartDeck => startDeck;

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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;     // 사라질 중복 오브젝트가 체력을 덮어쓰지 않게
        }

        if (character != null) currentHP = character.maxHp;
    }

    private void Start()
    {
        if (deck.Count == 0)
        {
            DeckInit();
        }
    }

    public void DeckInit()
    {
        deck.Clear();

        // 같은 객체를 넣으면 강화(upgradeLevel++)가 시작 덱의 카드까지 바꾸므로, 새 CardInstance 로 복사해서 넣음
        foreach (var card in startDeck)
        {
            if (card == null || card.baseData == null) continue;
            deck.Add(new CardInstance(card.baseData, card.upgradeLevel));
        }
    }

    public void StatInit()
    {
        character = DataManager.Instance.GetCharacter(1001);
        currentHP = character.maxHp;

        currentGold = 0;
    }
}
