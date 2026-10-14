using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 특정 기준 등급 이상의 아이템이 등장하지 않을 경우,
/// 상위 등급의 가중치를 점진적으로 누적 증가시키고(소프트 보정)
/// 고등급 등장 시 가중치를 초기화하는 확률 보정 클래스
/// </summary>
/// <typeparam name="T">추첨할 등급 타입 (Enum)</typeparam>
public class SoftPityProbabilityCorrector<T>
{
    private Dictionary<T, float> originalWeights; // 원본 기준 가중치 테이블
    private Dictionary<T, float> currentWeights;  // 실시간 보정 가중치 테이블

    private const float WEIGHT_INCREASE_AMOUNT = 0.5f; // 미등장 시 가중치 증가량
    private int startRank; // 보정 적용 기준 시작 등급 (정수값)

    /// <summary>
    /// 생성자: 기본 가중치 테이블과 보정 시작 등급을 받아 초기화합니다.
    /// </summary>
    /// <param name="initialWeights">항목별 가중치 딕셔너리</param>
    /// <param name="startRank">보정을 시작할 최소 등급 (int)</param>
    public SoftPityProbabilityCorrector(Dictionary<T, float> initialWeights, int startRank)
    {
        this.startRank = startRank;
        SetWeightTable(initialWeights);
    }

    /// <summary>
    /// 가중치 테이블을 설정하고 현재 가중치를 원본과 동일하게 초기화합니다. (챕터 전환 시 사용)
    /// </summary>
    public void SetWeightTable(Dictionary<T, float> newWeights)
    {
        originalWeights = new Dictionary<T, float>(newWeights);
        ResetWeights();
    }

    /// <summary>
    /// 현재 가중치를 원본 가중치 값으로 리셋합니다.
    /// </summary>
    public void ResetWeights()
    {
        currentWeights = new Dictionary<T, float>(originalWeights);
    }

    /// <summary>
    /// 현재 가중치를 바탕으로 룰렛 추첨을 수행하고,
    /// 결과에 따라 가중치 리셋 또는 소프트 보정을 적용합니다.
    /// </summary>
    /// <returns>추첨된 등급 Key</returns>
    public T EvaluateNext()
    {
        // 1. 현재 가중치 총합 검증
        float totalCurrentWeight = currentWeights.Values.Sum();
        if (totalCurrentWeight <= 0f)
        {
            Debug.LogError("[SoftPityProbabilityCorrector] 현재 가중치의 합이 0 이하입니다!");
            return currentWeights.Keys.First();
        }

        // 2. 현재 가중치 기반으로 즉시 추첨
        T selectedItem = RollByWeight(currentWeights);

        // 3. 고등급 추첨 여부 판별
        int currentRank = Convert.ToInt32(selectedItem);
        bool isHighRankSuccess = currentRank < startRank ? false : true;

        // 4. 추첨 결과에 따른 사후 처리
        if (isHighRankSuccess)
        {
            // 높은 등급 획득 성공 -> 기본 가중치로 리셋
            ResetWeights();
        }
        else
        {
            // 높은 등급 미등장 -> 소프트 보정 진행
            ApplySoftPity();
        }

        // 일단 지금 뽑은거 리턴 후, 보정된 내용은 다음 추첨부터 반영
        return selectedItem;
    }

    /// <summary>
    /// 고등급 미등장 시 보정 대상 등급을 확률적으로 골라 가중치 0.5를 가산합니다.
    /// </summary>
    private void ApplySoftPity()
    {
        // Enum에 정의된 모든 정수 랭크 값 파싱 및 최대 랭크 확인
        int[] allRanks = Enum.GetValues(typeof(T))
            .Cast<T>()
            .Select(val => Convert.ToInt32(val))
            .OrderBy(val => val)
            .ToArray();

        if (allRanks.Length == 0) return;

        int maxRank = allRanks.Max();
        int targetCount = maxRank - startRank + 1;  // 보정할 항목 수

        if (targetCount <= 0) return;

        float randomValue = UnityEngine.Random.Range(0f, 100f);
        int targetRankInt;

        // 보정 항목 수(targetCount)에 따른 확률 하드코딩
        // 낮은 인덱스(기준 등급)에 85% 등 높은 확률을 배분하여 고티어 역전 현상 방지
        switch (targetCount)
        {
            case 1:
                targetRankInt = maxRank;
                break;

            case 2:
                // 예: 5등급(85%), 6등급(15%)
                targetRankInt = (randomValue < 85f)
                    ? startRank
                    : (startRank + 1);
                break;

            case 3:
                // 예: 4등급(85%), 5등급(13%), 6등급(2%)
                if (randomValue < 85f)
                    targetRankInt = startRank;
                else if (randomValue < 98f)
                    targetRankInt = startRank + 1;
                else
                    targetRankInt = startRank + 2;
                break;

            case 4:
                // 예: 3등급(85%), 4등급(12%), 5등급(2.5%), 6등급(0.5%)
                if (randomValue < 85f)
                    targetRankInt = startRank;
                else if (randomValue < 97f)
                    targetRankInt = startRank + 1;
                else if (randomValue < 99.5f)
                    targetRankInt = startRank + 2;
                else
                    targetRankInt = startRank + 3;
                break;

            default:
                Debug.LogWarning($"[SoftPityProbabilityCorrector] 지원하지 않는 보정 항목 수: {targetCount}. 최상위 등급으로 대체합니다.");
                targetRankInt = maxRank;
                break;
        }

        // 대상 정수 랭크를 Enum 타입으로 캐스팅 및 존재 여부 검증
        if (Enum.IsDefined(typeof(T), targetRankInt))
        {
            T targetRankType = (T)Enum.ToObject(typeof(T), targetRankInt);

            // 해당 등급이 가중치 딕셔너리에 등록되어 있다면 +0.5 가산
            if (currentWeights.ContainsKey(targetRankType))
            {
                currentWeights[targetRankType] += WEIGHT_INCREASE_AMOUNT;
            }
        }
        else
        {
            Debug.LogWarning($"[SoftPityProbabilityCorrector] 계산된 랭크({targetRankInt})가 Enum '{typeof(T).Name}'에 정의되어 있지 않습니다.");
        }
    }

    /// <summary>
    /// 가중치 딕셔너리 기반 누적합 룰렛 추첨
    /// </summary>
    private T RollByWeight(Dictionary<T, float> targetTable)
    {
        float totalWeight = targetTable.Values.Sum();
        if (totalWeight <= 0f) return targetTable.Keys.Last();

        float roll = UnityEngine.Random.Range(0f, totalWeight);
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
    /// 현재 보정 진행 상태 디버깅용 메서드
    /// </summary>
    public string GetStatusReport()
    {
        string report = $"[현재 가중치 현황 (보정 시작: {startRank}등급)]\n";
        foreach (var pair in currentWeights)
        {
            float orig = originalWeights[pair.Key];
            report += $"{pair.Key}: 현재 {pair.Value:F1} (기본 {orig:F1})\n";
        }
        return report;
    }
}