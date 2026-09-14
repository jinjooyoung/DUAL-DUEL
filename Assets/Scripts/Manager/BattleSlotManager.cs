using System.Collections.Generic;
using UnityEngine;

public class BattleSlotManager : MonoBehaviour
{
    [Header("고정 슬롯 5개 (좌측 0번 ~ 우측 4번)")]
    public List<BattleSlot> slots = new List<BattleSlot>();

    private ProbabilityCorrector<int> slotCorrector;

    void Awake()
    {
        // 10층 가중치: M0(0), M1(20), M2(35), M3(35), M4(10), M5(0)
        // M뒤의 숫자는 몬스터 슬롯 개수를 의미
        Dictionary<int, float> floor10Weights = new()
        {
            { 0, 0f },
            { 1, 20f },
            { 2, 35f },
            { 3, 35f },
            { 4, 10f },
            { 5, 0f }
        };

        // 보정 시스템 생성
        slotCorrector = new ProbabilityCorrector<int>(floor10Weights);
    }

    // 매 턴 호출
    public void GenerateTurnSlotTypes()
    {
        // 기획된 가중치로 점점 수렴하며 뽑힌 몬스터 슬롯 개수
        int enemyCount = slotCorrector.EvaluateNext();
        int playerCount = 5 - enemyCount;

        Debug.Log($"보정 슬롯 결과 -> 적: {enemyCount}개, 플레이어: {playerCount}개");
    }
}