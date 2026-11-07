using System.Collections.Generic;
using UnityEngine;

public class BattleSlotManager : MonoBehaviour
{
    public static BattleSlotManager Instance { get; private set; }

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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 작성해 둔 확률 보정 시스템 초기화
        slotCorrector = new ProbabilityCorrector<int>(floor10Weights);
    }

    void Start()
    {
        // 게임 시작 시 첫 턴 슬롯 타입 무작위 생성
        GenerateTurnSlotTypes();
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

                // 슬롯 타입에 따라 랜덤 기본 공격력 결정 (int Random.Range는 최댓값 미포함이므로 +1)
                OwnerType type = slotTypes[i];
                int randomAtk = (type == OwnerType.Player)
                    ? Random.Range(1, 9)   // 플레이어: 1 ~ 8
                    : Random.Range(5, 16); // 적: 5 ~ 15

                slots[i].SetSlotType(slotTypes[i], randomAtk); // 슬롯 타입 및 외형 갱신
            }
        }

        Debug.Log($"[슬롯 배정 완료] 플레이어 슬롯: {playerCount}개 | 적 슬롯: {enemyCount}개");

        // 5. 슬롯 배정 직후 몬스터 기믹 적용 (블라인드, 밸류 왜곡)
        ApplyMonsterSlotGimmicks();

        return (playerCount, enemyCount);
    }

    #region 몬스터 슬롯 기믹

    private void ApplyMonsterSlotGimmicks()
    {
        MonsterSO monster = GameManager.Instance != null ? GameManager.Instance.monster : null;
        if (monster == null) return;    // 배틀 씬 단독 테스트 등

        if (monster.TryGetGimmick(GimmickType.BlindSlot, out int blindCount))
            ApplyBlindSlots(blindCount);

        if (monster.TryGetGimmick(GimmickType.ScaleSlotValue, out int maxAbsValue))
            ApplyScaleSlotValue(maxAbsValue);
    }

    /// <summary>
    /// [BlindSlot] 무작위 슬롯 blindCount개를 물음표로 가림 (턴 종료 시 BattleManager가 공개)
    /// </summary>
    private void ApplyBlindSlots(int blindCount)
    {
        List<int> indices = GetShuffledSlotIndices();
        int count = Mathf.Clamp(blindCount, 0, indices.Count);

        for (int i = 0; i < count; i++)
        {
            slots[indices[i]].SetBlind(true);
        }

        Debug.Log($"[몬스터 기믹: BlindSlot] 슬롯 {count}개 블라인드");
    }

    /// <summary>
    /// [ScaleSlotValue] 슬롯 1~2개에 밸류 보정 부여.
    /// 절댓값 1 ~ maxAbsValue, 부호는 따로 뽑아서 0이 나오지 않게 함. 2개면 두 번째 슬롯은 부호만 반대
    /// </summary>
    private void ApplyScaleSlotValue(int maxAbsValue)
    {
        List<int> indices = GetShuffledSlotIndices();
        if (indices.Count == 0) return;

        int slotCount = Mathf.Min(Random.Range(1, 3), indices.Count);           // 1 또는 2
        int magnitude = Random.Range(1, Mathf.Max(1, maxAbsValue) + 1);        // 1 ~ maxAbsValue
        int sign = Random.Range(0, 2) == 0 ? -1 : 1;                           // 0: 음수, 1: 양수

        slots[indices[0]].SetValueModifier(sign * magnitude);
        if (slotCount == 2) slots[indices[1]].SetValueModifier(-sign * magnitude);

        Debug.Log($"[몬스터 기믹: ScaleSlotValue] {indices[0]}번 슬롯 {sign * magnitude:+0;-0}" +
                  (slotCount == 2 ? $", {indices[1]}번 슬롯 {-sign * magnitude:+0;-0}" : string.Empty));
    }

    /// <summary>
    /// 유효한 슬롯 인덱스를 Fisher-Yates로 섞어서 반환 (중복 없는 무작위 선택용)
    /// </summary>
    private List<int> GetShuffledSlotIndices()
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null) indices.Add(i);
        }

        for (int i = 0; i < indices.Count; i++)
        {
            int rand = Random.Range(i, indices.Count);
            (indices[i], indices[rand]) = (indices[rand], indices[i]);
        }

        return indices;
    }

    #endregion

    /// <summary>
    /// 층/스테이지 변경 시 새 가중치 테이블로 교체
    /// </summary>
    public void UpdateFloorWeights(Dictionary<int, float> newWeights)
    {
        slotCorrector?.SetWeightTable(newWeights);
    }
}