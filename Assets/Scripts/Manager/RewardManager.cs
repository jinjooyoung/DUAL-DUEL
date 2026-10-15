using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 승리 후 카드 보상 생성 및 덱 추가를 총괄하는 싱글톤 매니저
/// </summary>
public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    // 1. 챕터별 카드 보상 등급 가중치 테이블
    // 1챕터: 25, 45, 30, 18, 2, 0
    private readonly Dictionary<RankType, float> chapter1Weights = new()
    {
        { RankType.Common, 25f },
        { RankType.Uncommon, 45f },
        { RankType.Rare, 30f },
        { RankType.Epic, 18f },
        { RankType.Legendary, 2f },
        { RankType.Mythic, 0f }
    };

    // 2챕터: 15, 35, 44, 22.8, 3, 0.2
    private readonly Dictionary<RankType, float> chapter2Weights = new()
    {
        { RankType.Common, 15f },
        { RankType.Uncommon, 35f },
        { RankType.Rare, 44f },
        { RankType.Epic, 22.8f },
        { RankType.Legendary, 3f },
        { RankType.Mythic, 0.2f }
    };

    // 3챕터: 10, 25, 43, 42.6, 8.5, 0.9
    private readonly Dictionary<RankType, float> chapter3Weights = new()
    {
        { RankType.Common, 10f },
        { RankType.Uncommon, 25f },
        { RankType.Rare, 43f },
        { RankType.Epic, 42.6f },
        { RankType.Legendary, 8.5f },
        { RankType.Mythic, 0.9f }
    };

    // 2. 소프트 보정 추첨기 (선언만 진행, 생성은 Initialize에서 단 1회)
    private SoftPityProbabilityCorrector<RankType> rankCorrector;

    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // 최상위 루트 보장
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 게임 중 최초 1회 호출되어 추첨기를 1챕터 가중치 기반으로 생성합니다.
    /// (보정 시작 등급은 Rare(3등급) 기준으로 설정)
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;

        // 1챕터 기준 가중치 및 Rare(3) 이상 미등장 시 소프트 보정 적용
        rankCorrector = new SoftPityProbabilityCorrector<RankType>(chapter1Weights, (int)RankType.Rare);
        isInitialized = true;

        Debug.Log("<color=green>[RewardManager] 카드 보상 등급 보정 추첨기 초기화 완료 (1챕터 가중치 적용)</color>");
    }

    /// <summary>
    /// 챕터 전환 시 호출하여 가중치 테이블을 교체할 때 사용
    /// </summary>
    public void SetChapter(int chapter)
    {
        if (rankCorrector == null) Initialize();

        switch (chapter)
        {
            case 1:
                rankCorrector.SetWeightTable(chapter1Weights);
                break;
            case 2:
                rankCorrector.SetWeightTable(chapter2Weights);
                break;
            case 3:
                rankCorrector.SetWeightTable(chapter3Weights);
                break;
        }

        Debug.Log($"<color=cyan>[RewardManager] {chapter}챕터 가중치 테이블 교체 완료</color>");
    }
}