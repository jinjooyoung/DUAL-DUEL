using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 승리 후 카드 보상 데이터 및 새로고침 수량을 총괄하는 전역 싱글톤 매니저
/// </summary>
public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    [Header("챕터별 등급 가중치")]
    private readonly Dictionary<RankType, float> chapter1Weights = new()
    {
        { RankType.Common, 25f },
        { RankType.Uncommon, 45f },
        { RankType.Rare, 30f },
        { RankType.Epic, 18f },
        { RankType.Legendary, 2f },
        { RankType.Mythic, 0f }
    };

    private readonly Dictionary<RankType, float> chapter2Weights = new()
    {
        { RankType.Common, 15f },
        { RankType.Uncommon, 35f },
        { RankType.Rare, 44f },
        { RankType.Epic, 22.8f },
        { RankType.Legendary, 3f },
        { RankType.Mythic, 0.2f }
    };

    private readonly Dictionary<RankType, float> chapter3Weights = new()
    {
        { RankType.Common, 10f },
        { RankType.Uncommon, 25f },
        { RankType.Rare, 43f },
        { RankType.Epic, 42.6f },
        { RankType.Legendary, 8.5f },
        { RankType.Mythic, 0.9f }
    };

    private SoftPityProbabilityCorrector<RankType> rankCorrector;
    private bool isInitialized = false;

    [Header("현재 보상 추첨 데이터")]
    public List<CardSO> currentRewardCards = new List<CardSO>();

    [Header("전역 새로고침 제한")]
    public int remainRerollCount = 5; // 게임 전체 동안 총 5회 제한

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        if (isInitialized) return;

        rankCorrector = new SoftPityProbabilityCorrector<RankType>(chapter1Weights, (int)RankType.Rare);
        isInitialized = true;
        Debug.Log("<color=green>[RewardManager] 보상 추첨기 초기화 완료</color>");
    }

    public void SetChapter(int chapter)
    {
        if (rankCorrector == null) Initialize();

        switch (chapter)
        {
            case 1: rankCorrector.SetWeightTable(chapter1Weights); break;
            case 2: rankCorrector.SetWeightTable(chapter2Weights); break;
            case 3: rankCorrector.SetWeightTable(chapter3Weights); break;
        }
    }

    public RankType EvaluateRank()
    {
        if (rankCorrector == null) Initialize();
        return rankCorrector.EvaluateNext();
    }

    public CardSO GetRandomCardByRank(RankType rank)
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("<color=red>[RewardManager] DataManager.Instance가 null입니다!</color>");
            return null;
        }

        List<CardSO> cardList = DataManager.Instance.GetCardsByRank(rank);
        if (cardList == null || cardList.Count == 0)
        {
            Debug.LogWarning($"<color=yellow>[RewardManager] '{rank}' 등급 카드가 비어있습니다.</color>");
            return null;
        }

        int randomIndex = Random.Range(0, cardList.Count);
        return cardList[randomIndex];
    }

    /// <summary>
    /// 카드 3장을 일괄 추첨하여 임시 보상 리스트를 구성합니다.
    /// </summary>
    public void GenerateRewardCards()
    {
        currentRewardCards.Clear();

        for (int i = 0; i < 3; i++)
        {
            RankType rolledRank = EvaluateRank();
            CardSO card = GetRandomCardByRank(rolledRank);

            // 안전장치: 혹시라도 null이 나오면 한 번 더 Common으로 Fallback
            if (card == null)
            {
                card = GetRandomCardByRank(RankType.Common);
            }

            currentRewardCards.Add(card);
            Debug.Log($"<color=cyan>[RewardManager] {i}번 슬롯 카드 세팅: {card?.nameKey} (등급: {rolledRank})</color>");
        }
    }

    /// <summary>
    /// 특정 인덱스의 카드 슬롯만 새로 추첨하여 덮어씌웁니다.
    /// </summary>
    public bool RerollCardSlot(int index)
    {
        if (remainRerollCount <= 0)
        {
            Debug.LogWarning("[RewardManager] 남은 새로고침 횟수가 없습니다!");
            return false;
        }

        if (index < 0 || index >= currentRewardCards.Count)
        {
            Debug.LogError($"[RewardManager] 잘못된 리롤 인덱스입니다: {index}");
            return false;
        }

        // 새로고침 차감 및 새 카드 추출
        remainRerollCount--;
        RankType rolledRank = EvaluateRank();
        CardSO newCard = GetRandomCardByRank(rolledRank) ?? GetRandomCardByRank(RankType.Common);

        currentRewardCards[index] = newCard;
        Debug.Log($"<color=yellow>[RewardManager] {index}번 슬롯 새로고침 성공! -> {newCard.nameKey} (남은 횟수: {remainRerollCount})</color>");

        return true;
    }

    /// <summary>
    /// 플레이어가 카드를 선택했을 때 GameManager의 덱에 CardInstance 형태로 추가
    /// </summary>
    public void SelectCard(int index)
    {
        if (index < 0 || index >= currentRewardCards.Count) return;

        CardSO chosenSO = currentRewardCards[index];
        if (chosenSO != null)
        {
            CardInstance newInstance = new CardInstance(chosenSO, 0);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.deck.Add(newInstance);
                Debug.Log($"<color=green>[RewardManager] 덱 추가 완료! {chosenSO.nameKey} (현재 총 덱 수량: {GameManager.Instance.deck.Count})</color>");
            }
        }

        currentRewardCards.Clear();
    }
}