using System.Collections.Generic;
using UnityEngine;

public class BattleSlotManager : MonoBehaviour
{
    [Header("고정 슬롯 5개 (순서대로)")]
    public List<BattleSlot> slots = new List<BattleSlot>();

    // 10층 기준 슬롯 타입 가중치 테이블 (Key: 몬스터 슬롯 개수 0~5, Value: 가중치)
    private readonly Dictionary<int, float> floor10Weights = new()
    {
        { 0, 0f },
        { 1, 20f },
        { 2, 35f },
        { 3, 35f },
        { 4, 10f },
        { 5, 0f }
    };

    private ProbabilityCorrector<int> slotCorrector;

    void Awake()
    {
        // 작성해 둔 확률 보정 시스템 초기화
        slotCorrector = new ProbabilityCorrector<int>(floor10Weights);
    }

    /// <summary>
    /// 매 턴 시작 시 호출: 보정 시스템으로 타입을 결정하고 5개 슬롯에 무작위 배정
    /// </summary>
    public (int playerCount, int enemyCount) GenerateTurnSlotTypes()
    {
        // 1. 보정 클래스에서 이번 턴에 등장할 적 슬롯 개수(0~5) 추첨
        int enemyCount = slotCorrector.EvaluateNext();
        // 확률 클래스 디버깅용 코드 Debug.Log(slotCorrector.GetStatusReport());
        int playerCount = slots.Count - enemyCount;

        // 2. 슬롯 수량만큼 타입 리스트 구성
        List<OwnerType> slotTypes = new List<OwnerType>();
        for (int i = 0; i < enemyCount; i++) slotTypes.Add(OwnerType.Enemy);
        for (int i = 0; i < playerCount; i++) slotTypes.Add(OwnerType.Player);

        // 3. 슬롯 순서 무작위 셔플 (Fisher-Yates)
        for (int i = 0; i < slotTypes.Count; i++)
        {
            OwnerType temp = slotTypes[i];
            int rand = Random.Range(i, slotTypes.Count);
            slotTypes[i] = slotTypes[rand];
            slotTypes[rand] = temp;
        }

        // 4. 인스펙터에 연결된 고정 슬롯 5개에 타입 주입
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].ClearSlot();               // 이전 턴 잔여 카드 참조 초기화
                slots[i].SetSlotType(slotTypes[i]); // 슬롯 타입 및 외형 갱신
            }
        }

        Debug.Log($"[슬롯 배정 완료] 플레이어 슬롯: {playerCount}개 | 적 슬롯: {enemyCount}개");
        return (playerCount, enemyCount);
    }

    /// <summary>
    /// 층/스테이지 변경 시 새 가중치 테이블로 교체
    /// </summary>
    public void UpdateFloorWeights(Dictionary<int, float> newWeights)
    {
        slotCorrector?.SetWeightTable(newWeights);
    }
}