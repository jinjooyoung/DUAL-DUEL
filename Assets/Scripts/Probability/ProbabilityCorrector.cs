using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProbabilityCorrector<T>
{
    private Dictionary<T, float> weightTable;   // 목표 가중치 테이블
    private Dictionary<T, int> historyCounts;   // 항목별 등장 횟수 히스토리
    private int totalCount;                     // 전체 시도 횟수

    // 생성자: 기본 가중치 테이블을 받아 초기화
    public ProbabilityCorrector(Dictionary<T, float> initialWeights)
    {
        SetWeightTable(initialWeights);
    }

    // 가중치 테이블 재설정 (층/스테이지 변경 시 사용)
    public void SetWeightTable(Dictionary<T, float> newWeights)
    {
        weightTable = new Dictionary<T, float>(newWeights);
        ResetHistory();
    }

    // 히스토리 초기화
    public void ResetHistory()
    {
        totalCount = 0;
        historyCounts = new Dictionary<T, int>();

        foreach (var key in weightTable.Keys)
        {
            historyCounts[key] = 0;
        }
    }

    // [핵심 함수] 플로우 차트 기반의 보정 확률 추첨
    public T EvaluateNext()
    {
        // 1. 전체 가중치 합산
        float totalWeight = weightTable.Values.Sum();
        if (totalWeight <= 0f)
        {
            Debug.LogError("가중치의 합이 0 이하입니다!");
            return weightTable.Keys.First();
        }

        T selectedKey;

        // 2. 히스토리가 1회 이상 쌓였을 때만 비율 비교 검사 진행
        if (totalCount > 0)
        {
            float maxDeficit = 0f;
            T mostDeficitKey = default;
            bool hasDeficit = false;

            // 각 확률 대상 반복 검사 (Loop 시작)
            foreach (var pair in weightTable)
            {
                T key = pair.Key;
                float targetRate = pair.Value / totalWeight;                       // 목표 비율
                float currentRate = (float)historyCounts[key] / totalCount;        // 현재 비율

                // 조건: 현재 비율 < 목표 비율?
                if (currentRate < targetRate)
                {
                    float deficit = targetRate - currentRate; // 부족한 폭

                    // 가장 부족한 결과 선택 (목표 - 현재 비율이 가장 큰 것)
                    if (deficit > maxDeficit)
                    {
                        maxDeficit = deficit;
                        mostDeficitKey = key;
                        hasDeficit = true;
                    }
                }
            }

            // 부족한 대상이 존재하면 가장 부족한 항목 강제 선택
            if (hasDeficit)
            {
                selectedKey = mostDeficitKey;
            }
            else
            {
                // 부족한 항목이 없으면 순수 가중치 랜덤 뽑기
                selectedKey = RollByWeight(totalWeight);
            }
        }
        else
        {
            // 첫 번째 뽑기: 히스토리가 없으므로 가중치 기반 랜덤
            selectedKey = RollByWeight(totalWeight);
        }

        // 3. 히스토리 업데이트
        historyCounts[selectedKey]++;
        totalCount++;

        return selectedKey;
    }

    // 가중치 누적합 룰렛
    private T RollByWeight(float totalWeight)
    {
        float roll = Random.Range(0f, totalWeight);
        float accumulator = 0f;

        foreach (var pair in weightTable)
        {
            accumulator += pair.Value;

            // 누적값이 롤 값보다 크거나 같아지는 지점 반환
            if (roll <= accumulator)
            {
                return pair.Key;
            }
        }

        return weightTable.Keys.Last(); // fallback
    }

    // 현재까지의 등장 통계 확인용 (디버깅 / 시뮬레이션용)
    public string GetStatusReport()
    {
        if (totalCount == 0) return "히스토리 데이터 없음";

        float totalWeight = weightTable.Values.Sum();
        string report = $"[총 시도: {totalCount}회]\n";

        foreach (var pair in weightTable)
        {
            float target = (pair.Value / totalWeight) * 100f;
            float actual = ((float)historyCounts[pair.Key] / totalCount) * 100f;
            report += $"{pair.Key}: 목표 {target:F1}% | 실제 {actual:F1}% ({historyCounts[pair.Key]}회)\n";
        }
        return report;
    }
}