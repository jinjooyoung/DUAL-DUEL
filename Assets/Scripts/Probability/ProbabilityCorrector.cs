using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 가중치 테이블과 누적 히스토리를 기반으로 
/// 목표 비율보다 덜 등장한 항목의 가중치를 일시적으로 2배 높여 룰렛을 돌리는 제네릭 확률 보정 클래스
/// </summary>
/// <typeparam name="T">추첨할 항목의 타입 (int, string, Enum 등)</typeparam>
public class ProbabilityCorrector<T>
{
    private Dictionary<T, float> weightTable;   // 원본 기준 가중치 테이블
    private Dictionary<T, int> historyCounts;   // 항목별 누적 등장 횟수
    private int totalCount;                     // 전체 시도 횟수

    // 부족한 항목에 부여할 가중치 배율 (기본 2배)
    private const float BOOST_MULTIPLIER = 2.0f;

    /// <summary>
    /// 기본 가중치 테이블을 받아 초기화하고 보정기를 생성합니다.
    /// </summary>
    /// <param name="initialWeights">항목별 가중치 딕셔너리</param>
    public ProbabilityCorrector(Dictionary<T, float> initialWeights)
    {
        SetWeightTable(initialWeights);
    }

    /// <summary>
    /// 새로운 가중치 테이블로 변경하고 기존 히스토리를 초기화합니다. (층/스테이지 변경 시 사용)
    /// </summary>
    /// <param name="newWeights">새로 적용할 가중치 딕셔너리</param>
    public void SetWeightTable(Dictionary<T, float> newWeights)
    {
        weightTable = new Dictionary<T, float>(newWeights);
        ResetHistory();
    }

    /// <summary>
    /// 전체 시도 횟수와 항목별 등장 횟수를 0으로 리셋합니다.
    /// </summary>
    public void ResetHistory()
    {
        totalCount = 0;
        historyCounts = new Dictionary<T, int>();

        foreach (var key in weightTable.Keys)
        {
            historyCounts[key] = 0;
        }
    }

    /// <summary>
    /// [동적 가중치 룰렛 추첨]
    /// 목표 비율 대비 부족한 항목들의 가중치를 일시적으로 2배 높인 임시 테이블을 만들어 추첨
    /// </summary>
    /// <returns>선출된 항목 Key</returns>
    public T EvaluateNext()
    {
        float totalBaseWeight = weightTable.Values.Sum();
        if (totalBaseWeight <= 0f)
        {
            Debug.LogError("가중치의 합이 0 이하입니다!");
            return weightTable.Keys.First();
        }

        // 이번 추첨에만 사용할 임시 가중치 딕셔너리
        Dictionary<T, float> runtimeWeightTable = new Dictionary<T, float>();

        if (totalCount > 0)
        {
            // 각 항목별로 결핍 여부를 따져 가중치 2배 부스팅 적용
            foreach (var pair in weightTable)
            {
                T key = pair.Key;
                float targetRate = pair.Value / totalBaseWeight;                    // 목표 비율
                float currentRate = (float)historyCounts[key] / totalCount;        // 실제 등장 비율

                // 실제 비율이 목표보다 낮다면 가중치 2배 버프 적용
                if (currentRate < targetRate && pair.Value > 0f)
                {
                    runtimeWeightTable[key] = pair.Value * BOOST_MULTIPLIER;
                }
                else
                {
                    runtimeWeightTable[key] = pair.Value;
                }
            }
        }
        else
        {
            // 첫 1회차: 원본 가중치 그대로 사용
            foreach (var pair in weightTable)
            {
                runtimeWeightTable[key: pair.Key] = pair.Value;
            }
        }

        // 2. 동적으로 조정된 런타임 가중치 테이블로 룰렛 추첨
        T selectedKey = RollByWeight(runtimeWeightTable);

        // 3. 히스토리 업데이트
        historyCounts[selectedKey]++;
        totalCount++;

        return selectedKey;
    }

    /// <summary>
    /// 전달받은 가중치 딕셔너리의 누적합(Prefix Sum)을 기반으로 무작위 추첨을 수행합니다.
    /// </summary>
    /// <param name="targetTable">추첨에 사용할 가중치 테이블 (임시 부스팅 테이블 등)</param>
    /// <returns>당첨된 항목 Key</returns>
    private T RollByWeight(Dictionary<T, float> targetTable)
    {
        float totalWeight = targetTable.Values.Sum();
        if (totalWeight <= 0f) return targetTable.Keys.Last();

        float roll = Random.Range(0f, totalWeight);
        float accumulator = 0f;

        foreach (var pair in targetTable)
        {
            accumulator += pair.Value;

            if (roll <= accumulator)
            {
                return pair.Key;
            }
        }

        return targetTable.Keys.Last(); // fallback
    }

    /// <summary>
    /// 현재까지의 시도 횟수와 목표 비율 대비 실제 달성 비율을 포맷팅된 문자열로 반환합니다.
    /// </summary>
    /// <returns>디버깅 및 밸런스 검증용 리포트 문자열</returns>
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