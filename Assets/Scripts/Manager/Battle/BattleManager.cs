using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 버프 / 디버프 종류. 정수값 = UI 아이콘 리스트 인덱스 (BattleUIManager의 BuffIcons 순서와 동일하게 유지)
/// </summary>
public enum StatusEffectType
{
    Regeneration = 0,   // [버프] 재생 : 턴 종료 시 체력 +n
    Force = 1,          // [버프] 힘 : 공격 피해량 +n
    Lifesteal = 2,      // [버프] 흡혈 : 체력에 들어간 피해의 n% 회복 (최대 100)
    Poison = 3,         // [디버프] 중독 : 턴 종료 시 피해 n
    Bleed = 4,          // [디버프] 출혈 : 공격받을 때 피해 +n
    Weaken = 5          // [디버프] 약화 : 카드 밸류 -n
}

[Serializable]
public class CombatEntityStats
{
    public const int STATUS_DURATION = 3;   // 버프/디버프 시전 시 덮어씌울 유지 턴수
    public const int LIFESTEAL_MAX = 100;   // 흡혈 최대치 (%)
    public const int STATUS_COUNT = 6;      // StatusEffectType 개수

    [Tooltip("현재 체력")] public int currentHp;
    [Tooltip("최대 체력")] public int maxHp;
    [Tooltip("현재 방어도")] public int guard;

    [Header("버프 수치")]
    [Tooltip("재생: 턴 종료 시 회복량")] public int regeneration;
    [Tooltip("힘: 공격 피해 가산")] public int force;
    [Tooltip("흡혈: 체력 피해의 n% 회복")] public int lifesteal;

    [Header("디버프 수치")]
    [Tooltip("중독: 턴 종료 시 피해량")] public int poison;
    [Tooltip("출혈: 피격 시 추가 피해")] public int bleed;
    [Tooltip("약화: 카드 밸류 감소")] public int weaken;

    [Header("버프 유지 턴수")]
    public int regenerationTurns;
    public int forceTurns;
    public int lifestealTurns;

    [Header("디버프 유지 턴수")]
    public int poisonTurns;
    public int bleedTurns;
    public int weakenTurns;

    public CombatEntityStats(int maxHp)
    {
        this.maxHp = maxHp;
        this.currentHp = maxHp;
        this.guard = 0;
        ClearAllStatus();
    }

    public static bool IsBuff(StatusEffectType type) => type <= StatusEffectType.Lifesteal;

    public int GetStatusValue(StatusEffectType type) => StatusValueRef(type);
    public int GetStatusTurns(StatusEffectType type) => StatusTurnsRef(type);

    /// <summary>
    /// 버프/디버프 시전: 수치 누적 + 유지 턴수 3으로 덮어씌움. 실제로 증가한 수치를 반환
    /// </summary>
    public int AddStatus(StatusEffectType type, int amount)
    {
        ref int value = ref StatusValueRef(type);
        ref int turns = ref StatusTurnsRef(type);

        int before = value;
        value += amount;

        // 흡혈은 100%가 최대치 (이미 100이면 더 이상 증가하지 않음)
        if (type == StatusEffectType.Lifesteal)
            value = Mathf.Min(value, LIFESTEAL_MAX);

        turns = STATUS_DURATION;
        return value - before;
    }

    /// <summary>
    /// 모든 버프/디버프 유지 턴수 -1. 0이 된 효과는 수치도 0으로 초기화
    /// </summary>
    public void TickStatusDurations()
    {
        for (int i = 0; i < STATUS_COUNT; i++)
        {
            StatusEffectType type = (StatusEffectType)i;
            ref int turns = ref StatusTurnsRef(type);
            if (turns < 1) continue;

            turns--;
            if (turns == 0)
            {
                StatusValueRef(type) = 0;
            }
        }
    }

    public void ClearAllStatus()
    {
        for (int i = 0; i < STATUS_COUNT; i++)
        {
            StatusValueRef((StatusEffectType)i) = 0;
            StatusTurnsRef((StatusEffectType)i) = 0;
        }
    }

    // 개별 int 필드를 타입으로 접근하기 위한 참조 반환 (get/set 스위치문 중복 방지)
    private ref int StatusValueRef(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Regeneration: return ref regeneration;
            case StatusEffectType.Force:        return ref force;
            case StatusEffectType.Lifesteal:    return ref lifesteal;
            case StatusEffectType.Poison:       return ref poison;
            case StatusEffectType.Bleed:        return ref bleed;
            case StatusEffectType.Weaken:       return ref weaken;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private ref int StatusTurnsRef(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Regeneration: return ref regenerationTurns;
            case StatusEffectType.Force:        return ref forceTurns;
            case StatusEffectType.Lifesteal:    return ref lifestealTurns;
            case StatusEffectType.Poison:       return ref poisonTurns;
            case StatusEffectType.Bleed:        return ref bleedTurns;
            case StatusEffectType.Weaken:       return ref weakenTurns;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}

// 배틀 시 핸드 덱을 배치 및 사용하는 배틀의 큰 흐름을 관리하는 매니저
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("전투 슬롯 필드")]
    public List<BattleSlot> fieldCardSlots = new List<BattleSlot>();

    [Header("전투 주체 스탯")]
    public CombatEntityStats playerStats;
    public CombatEntityStats monsterStats;

    [Header("전투 주체 위치 (플로팅 텍스트 기준점)")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform monsterTransform;

    /// <summary>
    /// 대상 스탯(Entity)을 기반으로 플로팅 텍스트가 뜰 월드 위치를 가져옵니다.
    /// </summary>
    private Vector3 GetEntityWorldPosition(CombatEntityStats entity)
    {
        if (entity == playerStats)
        {
            return playerTransform != null
                ? playerTransform.position + Vector3.down * 2.0f
                : new Vector3(-5f, 0f, 0f); // Fallback 기본 좌표
        }
        else
        {
            return monsterTransform != null
                ? monsterTransform.position + Vector3.down * 2.0f
                : new Vector3(5f, 0f, 0f);  // Fallback 기본 좌표
        }
    }

    private Transform GetEntityTransform(CombatEntityStats entity)
    {
        return entity == playerStats ? playerTransform : monsterTransform;
    }

    private CombatEntityStats GetOpponent(CombatEntityStats entity)
    {
        return entity == playerStats ? monsterStats : playerStats;
    }

    [Header("카메라 쉐이크")]
    [SerializeField] private RectTransform uiShakeRoot;
    [SerializeField] private Transform cameraTransform;

    [Header("카드 시전 딜레이")]
    [SerializeField] private float slotActionDelay = 0.5f;

    [Header("재생/중독 정산 연출 대기 시간")]
    [SerializeField] private float statusTickDelay = 0.4f;

    [Header("턴 전환 대기 시간")]
    [SerializeField] private float nextTurnDelay = 2.0f; // 턴 종료 후 다음 드로우까지 대기 시간 (2초)

    public bool isGameOver = false;
    public bool isTurnExecuting = false;

    private Coroutine turnExecutionCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitCombatStats();
        cameraTransform = Camera.main.transform;
    }

    // 순서 보장을 위해 초기화 로직 모음
    private void Start()
    {
        BattleUIManager.Instance.UpdateAllUI();
    }

    /// <summary>
    /// GameManager에 지정된 CharacterSO와 MonsterSO의 maxHp 데이터를 읽어 전투 스탯 초기화
    /// </summary>
    public void InitCombatStats()
    {
        if (GameManager.Instance != null)
        {
            // 플레이어 SO 데이터 연결 (없을 경우 50 기본값 안전장치)
            int pMaxHp = GameManager.Instance.character != null ? GameManager.Instance.currentHP : 50;
            playerStats = new CombatEntityStats(pMaxHp);

            // 몬스터 SO 데이터 연결 (없을 경우 35 기본값 안전장치)
            int mMaxHp = GameManager.Instance.monster != null ? GameManager.Instance.monster.maxHp : 35;
            monsterStats = new CombatEntityStats(mMaxHp);

            Debug.Log($"[전투 스탯 초기화 완료] 플레이어 HP: {playerStats.currentHp}/{playerStats.maxHp} | 몬스터 HP: {monsterStats.currentHp}/{monsterStats.maxHp}");
        }
        else
        {
            // 단독 씬 테스트용 Fallback
            playerStats = new CombatEntityStats(50);
            monsterStats = new CombatEntityStats(35);
        }
    }

    /// <summary>
    /// 턴 종료 버튼 클릭 시 호출
    /// </summary>
    public void TurnEnd()
    {
        if (turnExecutionCoroutine != null) return;

        if (isGameOver) return;

        if (isTurnExecuting) return;

        isTurnExecuting = true;

        /*// 5개 슬롯 완충 여부 검사
        if (!IsAllSlotsOccupied())
        {
            Debug.LogWarning("[턴 종료 불가] 모든 슬롯에 카드를 배치해야 턴을 마칠 수 있습니다.");
            return;
        }*/

        turnExecutionCoroutine = StartCoroutine(Co_ExecuteTurnSlots());
    }

    /// <summary>
    /// 모든 슬롯이 정상적으로 카드를 장착하고 있는지 확인
    /// </summary>
    private bool IsAllSlotsOccupied()
    {
        // 슬롯 리스트가 비어있거나 5개가 아니면 실행 불가
        if (fieldCardSlots == null || fieldCardSlots.Count < 5) return false;

        foreach (var slot in fieldCardSlots)
        {
            if (slot == null || !slot.isOccupied || slot.currentCard == null)
            {
                return false; // 빈 슬롯 발견 시 즉시 false 반환
            }
        }

        return true;
    }

    private IEnumerator Co_ExecuteTurnSlots()
    {
        // 0. 카드 시전 전 재생 -> 중독 정산
        yield return StartCoroutine(Co_ApplyTurnEndStatusEffects());
        if (isGameOver) yield break;

        // 1. 슬롯 5개 순차 발동 (1초 간격)
        for (int i = 0; i < fieldCardSlots.Count; i++)
        {
            if (isGameOver) yield break;

            BattleSlot slot = fieldCardSlots[i];
            if (slot == null) continue;

            // [분기 1] 슬롯에 카드가 장착되어 있는 경우 -> 기존 카드 효과 발동
            if (slot.isOccupied && slot.currentCard != null)
            {
                yield return StartCoroutine(ExecuteSlotAction(slot));
            }
            // [분기 2] 슬롯이 비어있는 경우 (카드가 null) -> 슬롯 자체의 기본 공격력 실행
            else
            {
                yield return StartCoroutine(Co_ExecuteSlotBaseAttack(slot));
            }

            slot.ClearAtkText();

            yield return new WaitForSeconds(slotActionDelay);

            // 적 사망 시 슬롯 루프 조기 종료
            if (monsterStats.currentHp <= 0)
            {
                isTurnExecuting = false;
                break;
            }
        }

        if (isGameOver) yield break;

        // 1-0. 모든 카드 시전 종료 -> 버프/디버프 유지 턴수 일괄 -1 (0이 되면 수치 초기화)
        playerStats.TickStatusDurations();
        monsterStats.TickStatusDurations();
        BattleUIManager.Instance?.UpdateAllUI();

        // [추가] 1-1. 0~4번 슬롯 및 모든 연계 시전이 완료된 후, 슬롯의 카드들을 0.2초 간격으로 순차 디스카드
        for (int i = 0; i < fieldCardSlots.Count; i++)
        {
            BattleSlot slot = fieldCardSlots[i];
            if (slot != null && slot.isOccupied && slot.currentCard != null)
            {
                CardDisplay cardToDiscard = slot.currentCard;

                // 슬롯 비우기
                slot.ClearSlot();

                // 디스카드 실행 (BattleCardManager)
                BattleCardManager.Instance?.DiscardCard(cardToDiscard);

                // 0.2초 간격 대기
                yield return new WaitForSeconds(0.2f);
            }
        }

        // 2. 슬롯 발동 완료 후, 손패에 남아있는 잉여 카드들 0.5초 간격으로 모두 버림
        if (BattleCardManager.Instance != null)
        {
            yield return StartCoroutine(BattleCardManager.Instance.Co_DiscardAllHandCards());
        }

        TurnResetGuard();

        isTurnExecuting = false;

        // 3. 손패 정리 완료 후 2초 대기 (턴 전환 딜레이 연출)
        yield return new WaitForSeconds(nextTurnDelay);

        // 4. 모든 정산 완료 및 새 턴 시작
        OnAllSlotsFinished();
        turnExecutionCoroutine = null;
    }

    /// <summary>
    /// 턴 종료 버튼 직후, 카드 시전 전에 재생 회복 -> 중독 피해 순서로 정산
    /// </summary>
    private IEnumerator Co_ApplyTurnEndStatusEffects()
    {
        // 1. 재생 (각 엔티티가 자기 재생 수치만큼 회복)
        bool isRegenApplied = false;
        isRegenApplied |= ApplyRegeneration(playerStats);
        isRegenApplied |= ApplyRegeneration(monsterStats);

        if (isRegenApplied)
        {
            BattleUIManager.Instance?.UpdateAllUI();
            yield return new WaitForSeconds(statusTickDelay);
        }

        // 2. 중독 (각 엔티티가 자기 중독 수치만큼 피해. 방어도 무시)
        bool isPoisonApplied = false;
        isPoisonApplied |= ApplyPoison(playerStats);
        isPoisonApplied |= ApplyPoison(monsterStats);

        if (isPoisonApplied)
        {
            BattleUIManager.Instance?.UpdateAllUI();
            yield return new WaitForSeconds(statusTickDelay);
        }
    }

    private bool ApplyRegeneration(CombatEntityStats entity)
    {
        if (isGameOver || entity.regeneration <= 0) return false;

        Debug.Log($"[재생] {(entity == playerStats ? "플레이어" : "몬스터")} 체력 +{entity.regeneration}");
        ModifyHealth(entity, entity.regeneration);
        return true;
    }

    private bool ApplyPoison(CombatEntityStats entity)
    {
        if (isGameOver || entity.poison <= 0) return false;

        Debug.Log($"[중독] {(entity == playerStats ? "플레이어" : "몬스터")} 피해 {entity.poison}");
        ModifyHealth(entity, -entity.poison, false);
        return true;
    }

    /// <summary>
    /// 카드가 비어 있는 슬롯에서 슬롯 고유의 기본 공격력을 시전하는 코루틴
    /// </summary>
    private IEnumerator Co_ExecuteSlotBaseAttack(BattleSlot slot)
    {
        if (isGameOver) yield break;

        bool isPlayerSlot = slot.slotOwnerType == OwnerType.Player;
        CombatEntityStats user = isPlayerSlot ? playerStats : monsterStats;
        CombatEntityStats target = isPlayerSlot ? monsterStats : playerStats;

        int rawAtk = slot.slotBaseAttack;

        // 카드가 아니므로 약화는 적용되지 않음. 힘/출혈은 ApplyDamage에서 가산
        int finalDamage = Mathf.Max(0, rawAtk);

        Debug.Log($"[빈 슬롯 기본 공격] {(isPlayerSlot ? "플레이어" : "적")} {slot.slotIndex}번 슬롯이 비어있어 기본 피해 {finalDamage}(원래: {rawAtk}) 시전!");

        // 슬롯 펀치/돌진 연출
        Sequence sequence = DOTweenManager.CardExecute(
            slot.transform,
            isPlayerSlot ? Vector3.right : Vector3.left
        );

        yield return sequence.WaitForCompletion();

        // 피해 적용
        ApplyDamage(target, finalDamage);
        BattleUIManager.Instance?.UpdateAllUI();
    }

    /// <summary>
    /// 카드의 조건(ConditionCategory) 충족 여부를 판별합니다.
    /// 각 케이스는 가지치기 방식: 실패 조건을 위에서부터 걸러내고(break), 끝까지 살아남으면 isConditionMet = true
    /// conditionParam 포맷 (Prev/Next/Neighbor): "슬롯주인,카드타입"  예) "Enemy,Attack", "Any,Defense", "Player,Any"
    /// </summary>
    private bool CheckCardCondition(BattleSlot slot, CardSO cardData)
    {
        bool isConditionMet = false;
        int currentIdx = slot.slotIndex;
        int lastIdx = fieldCardSlots.Count - 1;
        string param = cardData.conditionParam;

        switch (cardData.conditionCategory)
        {
            // 0. 조건 없음: 항상 참 (보상이 무조건 발동하는 카드용)
            case ConditionCategory.None:
                isConditionMet = true;
                break;

            // 4. 현재 슬롯 인덱스 검사 ("0" 또는 "0,4")
            case ConditionCategory.SlotIndex:
                if (string.IsNullOrEmpty(param)) break;

                foreach (string idxStr in param.Split(','))
                {
                    if (int.TryParse(idxStr.Trim(), out int targetIdx) && currentIdx == targetIdx)
                    {
                        isConditionMet = true;
                        break;
                    }
                }
                break;

            // 2. 이전 슬롯 검사
            case ConditionCategory.PrevCard:
            {
                // 가지치기 1: 맨 앞 슬롯은 이전 슬롯이 없음
                if (currentIdx <= 0) break;

                // 가지치기 2: 이전 슬롯에 카드가 없음
                BattleSlot prevSlot = fieldCardSlots[currentIdx - 1];
                if (!HasCard(prevSlot)) break;

                // 가지치기 3: 파라미터 형식 오류
                if (!TrySplitSlotParam(param, cardData, out string ownerParam, out string typeParam)) break;

                // 가지치기 4: 슬롯 주인 -> 카드 타입 순서로 검사
                isConditionMet = IsSlotMatch(prevSlot, ownerParam, typeParam);
                break;
            }

            // 3. 다음 슬롯 검사 (PrevCard와 동일, 방향만 반대)
            case ConditionCategory.NextCard:
            {
                if (currentIdx >= lastIdx) break;

                BattleSlot nextSlot = fieldCardSlots[currentIdx + 1];
                if (!HasCard(nextSlot)) break;

                if (!TrySplitSlotParam(param, cardData, out string ownerParam, out string typeParam)) break;

                isConditionMet = IsSlotMatch(nextSlot, ownerParam, typeParam);
                break;
            }

            // 1. 양옆 슬롯 모두 검사 (Prev + Next)
            case ConditionCategory.NeighborCard:
            {
                // 가지치기 1: 양 끝 슬롯은 이웃이 한쪽뿐
                if (currentIdx <= 0 || currentIdx >= lastIdx) break;

                // 가지치기 2: 양옆 중 하나라도 카드가 없음
                BattleSlot prevSlot = fieldCardSlots[currentIdx - 1];
                BattleSlot nextSlot = fieldCardSlots[currentIdx + 1];
                if (!HasCard(prevSlot) || !HasCard(nextSlot)) break;

                if (!TrySplitSlotParam(param, cardData, out string ownerParam, out string typeParam)) break;

                // 가지치기 3: 앞 슬롯이 실패하면 뒤 슬롯은 검사하지 않음 (&& 단락 평가)
                isConditionMet = IsSlotMatch(prevSlot, ownerParam, typeParam)
                              && IsSlotMatch(nextSlot, ownerParam, typeParam);
                break;
            }
        }

        return isConditionMet;
    }

    /// <summary>
    /// 슬롯에 카드가 정상적으로 배치되어 있는지
    /// </summary>
    private bool HasCard(BattleSlot targetSlot)
    {
        return targetSlot != null && targetSlot.isOccupied && targetSlot.currentCard != null
            && targetSlot.currentCard.cardInstance != null && targetSlot.currentCard.cardInstance.baseData != null;
    }

    /// <summary>
    /// "Enemy,Attack" -> ownerParam = "Enemy", typeParam = "Attack"
    /// </summary>
    private bool TrySplitSlotParam(string param, CardSO cardData, out string ownerParam, out string typeParam)
    {
        ownerParam = string.Empty;
        typeParam = string.Empty;

        if (string.IsNullOrEmpty(param)) return Fail();

        string[] split = param.Split(',');
        if (split.Length != 2) return Fail();

        ownerParam = split[0].Trim();
        typeParam = split[1].Trim();
        return true;

        bool Fail()
        {
            Debug.LogWarning($"[조건 파라미터 오류] 카드 {cardData.cardId} ({cardData.conditionCategory}) -> '{param}' 는 \"슬롯주인,카드타입\" 형식이어야 합니다.");
            return false;
        }
    }

    /// <summary>
    /// 대상 슬롯의 주인(A)과 배치된 카드 타입(B)을 순서대로 검사. 하나라도 어긋나면 즉시 false
    /// A: Enemy / Player / Any,  B: CardType 이름 / Any
    /// </summary>
    private bool IsSlotMatch(BattleSlot targetSlot, string ownerParam, string typeParam)
    {
        // A. 슬롯 주인 검사 (Any면 검사 생략)
        if (ownerParam == "Enemy")
        {
            if (targetSlot.slotOwnerType != OwnerType.Enemy) return false;
        }
        else if (ownerParam == "Player")
        {
            if (targetSlot.slotOwnerType != OwnerType.Player) return false;
        }
        else if (ownerParam != "Any")
        {
            Debug.LogWarning($"[조건 파라미터 오류] 슬롯 주인 '{ownerParam}' 는 Enemy / Player / Any 중 하나여야 합니다.");
            return false;
        }

        // B. 카드 타입 검사 (Any면 통과)
        if (typeParam == "Any") return true;

        if (!Enum.TryParse(typeParam, true, out CardType requiredType))
        {
            Debug.LogWarning($"[조건 파라미터 오류] 카드 타입 '{typeParam}' 를 CardType으로 변환할 수 없습니다.");
            return false;
        }

        return targetSlot.currentCard.cardInstance.baseData.cardType == requiredType;
    }

    /// <summary>
    /// 한 턴의 모든 연출과 버리기가 끝나고 새 턴이 시작될 때 호출
    /// </summary>
    private void OnAllSlotsFinished()
    {
        Debug.Log("[새 턴 시작] 다음 턴 슬롯 재배정 및 드로우 시작");

        // 다음 턴 슬롯 5개 타입(아군/적) 새로 배정
        BattleSlotManager.Instance?.GenerateTurnSlotTypes();

        // 기본 드로우 수 + 카드 효과로 적립된 보너스 드로우 합산 후 0으로 리셋
        if (BattleCardManager.Instance != null)
        {
            int finalDrawCount = BattleCardManager.Instance.drawCount + BattleCardManager.Instance.bonusDrawCount;
            BattleCardManager.Instance.bonusDrawCount = 0; // 드로우 직후 즉시 리셋

            Debug.Log($"[턴 시작 드로우] 기본 {BattleCardManager.Instance.drawCount}장 + 보너스 적용 -> 총 {finalDrawCount}장 드로우!");
            StartCoroutine(BattleCardManager.Instance.Co_DrawCards(finalDrawCount));
        }

        BattleUIManager.Instance?.UpdateAllUI();
    }

    private IEnumerator ExecuteSlotAction(BattleSlot slot)
    {
        if (isGameOver) yield break;

        CardDisplay cardDisplay = slot.currentCard;
        if (cardDisplay == null) yield break;

        CardInstance instance = cardDisplay.cardInstance;
        if (instance == null || instance.baseData == null) yield break;

        CardSO cardData = instance.baseData;

        bool isPlayerSlot = slot.slotOwnerType == OwnerType.Player;
        CombatEntityStats user = isPlayerSlot ? playerStats : monsterStats;
        CombatEntityStats target = isPlayerSlot ? monsterStats : playerStats;

        // 1. 조건 만족 여부 검사
        bool isConditionMet = CheckCardCondition(slot, cardData);
        string conditionStatus = isConditionMet ? "<color=#00FF00>[조건 달성 - 보상 활성화]</color>" : "<color=#FF4444>[조건 미달 - 기본 시전]</color>";
        Debug.Log($"[카드 효과 판별] 슬롯: {slot.slotIndex}번 | 카드 ID: {cardData.cardId} ({cardData.nameKey}) | 조건 타입: {cardData.conditionCategory} (파라미터: '{cardData.conditionParam}') -> {conditionStatus}");

        // 2. 카드의 기본 밸류 확인 (강화 수치 적용)
        int cardValue = instance.GetValue();

        // 2-1. 보상 파라미터 확인 (강화 단계 반영. upgradeTarget이 1인 카드만 단계별로 값이 다름)
        float rewardParam = instance.GetRewardParam();

        // 3. 시전 및 보상 관련 파라미터 초기화
        int repeatExtraCount = 0;
        int triggerNeighborTarget = -999; // -1: 이전, 1: 다음

        // 4. 조건 충족 시 보상 적용 (스위치문)
        if (isConditionMet)
        {
            switch (cardData.rewardCategory)
            {
                case RewardCategory.None:
                    break;

                // 정수 밸류 가산
                case RewardCategory.FlatBonus:
                    cardValue += Mathf.RoundToInt(rewardParam);
                    Debug.Log($"[보상: FlatBonus] {cardData.nameKey} 밸류 +{rewardParam} (최종: {cardValue})");
                    break;

                // 밸류 곱연산
                case RewardCategory.Multiplier:
                    cardValue = Mathf.RoundToInt(cardValue * rewardParam);
                    Debug.Log($"[보상: Multiplier] {cardData.nameKey} 밸류 x{rewardParam} (최종: {cardValue})");
                    break;

                // 다중 시전: 기본 1회 + 추가 rewardParam회
                case RewardCategory.RepeatCast:
                    repeatExtraCount = Mathf.Max(0, Mathf.RoundToInt(rewardParam));
                    Debug.Log($"[보상: RepeatCast] {cardData.nameKey} 추가 {repeatExtraCount}회 연타 시전 예약!");
                    break;

                // 인접 슬롯 카드 추가 시전 예약 (0: 이전 슬롯, 1: 다음 슬롯)
                case RewardCategory.TriggerNeighborSlot:
                    triggerNeighborTarget = Mathf.RoundToInt(rewardParam);
                    Debug.Log($"[보상: TriggerNeighborSlot] {(triggerNeighborTarget == 0 ? "이전" : "다음")} 슬롯 연계 시전 예약!");
                    break;

                // 다음 턴 드로우 매수 추가 적립
                case RewardCategory.ModifyNextDraw:
                    if (BattleCardManager.Instance != null)
                    {
                        int extraDraw = Mathf.RoundToInt(rewardParam);
                        BattleCardManager.Instance.bonusDrawCount += extraDraw;
                        Debug.Log($"[보상: ModifyNextDraw] 다음 턴 드로우 +{extraDraw}장 예약 (누적 추가: {BattleCardManager.Instance.bonusDrawCount})");
                    }
                    break;
            }
        }

        // 5. 기본 1회 시전 + RepeatCast 추가 횟수만큼 반복 실행
        int totalCasts = 1 + repeatExtraCount;

        for (int i = 0; i < totalCasts; i++)
        {
            // 실제 효과 발동 (약화 차감은 ApplyCardEffect에서 처리)
            int finalValue = Mathf.Max(0, cardValue);
            ApplyCardEffect(cardData.cardType, user, target, finalValue);

            // 사용 연출 (슬롯에 카드가 유지된 채로 찌르기)
            Sequence sequence = DOTweenManager.CardExecute(
                cardDisplay.transform,
                isPlayerSlot ? Vector3.right : Vector3.left
            );

            yield return sequence.WaitForCompletion();
            BattleUIManager.Instance?.UpdateAllUI();

            // 연타 시전 사이 짧은 연출 대기
            if (i < totalCasts - 1)
            {
                yield return new WaitForSeconds(0.15f);
            }

            // 시전 도중 적이 쓰러지면 즉시 연타 중단
            if (monsterStats.currentHp <= 0) break;
        }

        // 6. 인접 슬롯 연계 시전 처리 (TriggerNeighborSlot)
        if (triggerNeighborTarget != -999 && monsterStats.currentHp > 0)
        {
            int targetSlotIdx = (triggerNeighborTarget == 0) ? slot.slotIndex - 1 : slot.slotIndex + 1;

            if (targetSlotIdx >= 0 && targetSlotIdx < fieldCardSlots.Count)
            {
                BattleSlot neighborSlot = fieldCardSlots[targetSlotIdx];

                if (neighborSlot != null && neighborSlot.isOccupied && neighborSlot.currentCard != null)
                {
                    Debug.Log($"[연계 발동] {targetSlotIdx}번 슬롯의 {neighborSlot.currentCard.cardInstance.baseData.nameKey} 추가 시전 시작!");
                    yield return StartCoroutine(ExecuteNeighborSlotAction(neighborSlot));
                }
            }
        }

        // 7. 모든 다중 시전/연계가 완전히 끝난 후 최종 카드 회수 및 정리
        //BattleCardManager.Instance?.DiscardCard(cardDisplay);
        //slot.ClearSlot();

        BattleUIManager.Instance?.UpdateAllUI();
    }

    /// <param name="isAttack">false면 공격자 모션 없이 피격만 연출 (중독 등)</param>
    public void ModifyHealth(CombatEntityStats entity, int amount, bool isAttack = true)
    {
        entity.currentHp = Mathf.Clamp(entity.currentHp + amount, 0, entity.maxHp);
        Debug.Log($"HP 변경: {amount} (현재 HP: {entity.currentHp}/{entity.maxHp})");

        // 대상의 위치 자동 판별
        Vector3 spawnPos = GetEntityWorldPosition(entity);

        if (amount > 0)
        {
            BattleUIManager.Instance?.ShowHealText(spawnPos, amount);
            SoundManager.Instance.PlaySFX(SFXType.Heal);
            return;
        }
        else
        {
            BattleUIManager.Instance?.ShowDamageText(spawnPos, -amount);
            BattleVFXPlayer.Instance?.Play(GetEntityTransform(entity), VFXType.Hit);
            DOTweenManager.ShakeByDamage(-amount, uiShakeRoot, cameraTransform);
        }

        // 플레이어라면
        if (entity == playerStats)
        {
            SoundManager.Instance.PlaySFX(SFXType.Hit_Damage);
            if (isAttack) BattleUIManager.Instance.PlayMonsterAttack();
            BattleUIManager.Instance.PlayPlayerHit();
        }
        else
        {
            SoundManager.Instance.PlaySFX(SFXType.Attack_Slash);
            if (isAttack) BattleUIManager.Instance.PlayPlayerAttack();
            BattleUIManager.Instance.PlayMonsterHit();
        }

        if (entity.currentHp <= 0)
        {
            HandleDeath(entity);
        }
    }

    /// <summary>
    /// 카드 타입에 따른 실제 수치 효과 적용
    /// </summary>
    private void ApplyCardEffect(CardType cardType, CombatEntityStats user, CombatEntityStats target, int finalValue)
    {
        // [약화] 시전자의 약화 수치만큼 카드 밸류 감소 (최솟값 0)
        if (user.weaken > 0)
        {
            int before = finalValue;
            finalValue = Mathf.Max(0, finalValue - user.weaken);
            Debug.Log($"[약화] 카드 밸류 {before} -> {finalValue} (약화 {user.weaken})");
        }

        switch (cardType)
        {
            case CardType.Attack:
                ApplyDamage(target, finalValue);
                break;

            case CardType.Defense:
                ModifyGuard(user, finalValue);
                break;

            case CardType.Heal:
                if (finalValue > 0) ModifyHealth(user, finalValue);   // 0 회복은 ModifyHealth에서 피격 연출로 처리되므로 생략
                break;

            // 버프: 시전자 본인에게
            case CardType.Buff_Regeneration:
                ApplyStatusEffect(user, user, StatusEffectType.Regeneration, finalValue);
                break;

            case CardType.Buff_Force:
                ApplyStatusEffect(user, user, StatusEffectType.Force, finalValue);
                break;

            case CardType.Buff_Lifesteal:
                ApplyStatusEffect(user, user, StatusEffectType.Lifesteal, finalValue);
                break;

            // 디버프: 시전자의 상대에게
            case CardType.Debuff_Poison:
                ApplyStatusEffect(user, target, StatusEffectType.Poison, finalValue);
                break;

            case CardType.Debuff_Bleed:
                ApplyStatusEffect(user, target, StatusEffectType.Bleed, finalValue);
                break;

            case CardType.Debuff_Weaken:
                ApplyStatusEffect(user, target, StatusEffectType.Weaken, finalValue);
                break;
        }
    }

    /// <summary>
    /// TriggerNeighborSlot 보상으로 인해 호출되는 인접 슬롯 1회 단독 시전 코루틴
    /// (해당 카드의 디스카드 처리는 나중에 본인 차례에 하도록 슬롯에 그대로 둠)
    /// </summary>
    private IEnumerator ExecuteNeighborSlotAction(BattleSlot targetSlot)
    {
        CardDisplay cardDisplay = targetSlot.currentCard;
        if (cardDisplay == null || cardDisplay.cardInstance == null) yield break;

        CardSO cardData = cardDisplay.cardInstance.baseData;
        bool isPlayer = targetSlot.slotOwnerType == OwnerType.Player;
        CombatEntityStats user = isPlayer ? playerStats : monsterStats;
        CombatEntityStats target = isPlayer ? monsterStats : playerStats;

        int finalValue = Mathf.Max(0, cardDisplay.cardInstance.GetValue());

        ApplyCardEffect(cardData.cardType, user, target, finalValue);

        Sequence seq = DOTweenManager.CardExecute(
            cardDisplay.transform,
            isPlayer ? Vector3.right : Vector3.left
        );

        yield return seq.WaitForCompletion();
        BattleUIManager.Instance?.UpdateAllUI();
    }

    /// <summary>
    /// 공격 데미지 처리 (힘/출혈 가산 -> 방어도 소진 -> 초과분 체력 차감 + 공격자 흡혈)
    /// 공격자는 항상 target의 상대 (카드 공격, 슬롯 기본 공격 공통)
    /// </summary>
    public void ApplyDamage(CombatEntityStats target, int damage)
    {
        CombatEntityStats attacker = GetOpponent(target);

        // [힘] 공격자 힘 수치 가산 -> [출혈] 피격자 출혈 수치 가산
        int baseDamage = damage;
        damage += attacker.force;
        damage += target.bleed;

        if (attacker.force > 0 || target.bleed > 0)
            Debug.Log($"[피해 가산] {baseDamage} + 힘 {attacker.force} + 출혈 {target.bleed} = {damage}");

        if (damage <= 0) return;

        // 1. 방어도가 공격력 이상인 경우 (방어도만 차감, 체력 피해 없음 -> 흡혈 없음)
        if (target.guard >= damage)
        {
            target.guard -= damage;
            SoundManager.Instance.PlaySFX(SFXType.Shield_Block);
            BattleVFXPlayer.Instance?.Play(GetEntityTransform(target), VFXType.Defense);
            return;
        }

        // 2. 방어도를 넘는 초과분만 체력 피해
        int hpDamage = damage - target.guard;
        target.guard = 0;

        // [흡혈] 실제로 깎이는 체력 기준으로 회복 (피격자 처치 시 승리 처리 전에 회복이 반영되도록 먼저 적용)
        ApplyLifesteal(attacker, Mathf.Min(hpDamage, target.currentHp));

        ModifyHealth(target, -hpDamage);
    }

    /// <summary>
    /// 흡혈: 체력 피해량 * 흡혈% (정수 반올림)
    /// </summary>
    private void ApplyLifesteal(CombatEntityStats attacker, int hpDamage)
    {
        if (attacker.lifesteal <= 0 || hpDamage <= 0) return;

        // Mathf.RoundToInt는 0.5를 짝수 쪽으로 반올림(2.5 -> 2)하므로 일반 반올림으로 처리
        int heal = Mathf.FloorToInt(hpDamage * attacker.lifesteal / 100f + 0.5f);
        if (heal <= 0) return;

        Debug.Log($"[흡혈] 체력 피해 {hpDamage} x {attacker.lifesteal}% -> 회복 {heal}");
        ModifyHealth(attacker, heal);
    }

    public void ModifyGuard(CombatEntityStats entity, int amount)
    {
        entity.guard = Mathf.Max(0, entity.guard + amount);
        Debug.Log($"방어도 변경: {amount} (현재 방어도: {entity.guard})");

        Vector3 spawnPos = GetEntityWorldPosition(entity);
        BattleUIManager.Instance?.ShowShieldText(spawnPos, amount);
        BattleVFXPlayer.Instance?.Play(GetEntityTransform(entity), VFXType.Defense);
        SoundManager.Instance.PlaySFX(SFXType.Shield_Taking);

        if (entity == playerStats)
        {
            BattleUIManager.Instance.PlayPlayerAttack();
        }
        else
        {
            BattleUIManager.Instance.PlayMonsterAttack();
        }
    }

    /// <summary>
    /// 버프/디버프 부여 (수치 누적 + 유지 턴수 갱신)
    /// </summary>
    /// <param name="caster">시전자 (모션 연출용)</param>
    /// <param name="receiver">효과를 받는 대상 (버프: 시전자 본인, 디버프: 상대)</param>
    public void ApplyStatusEffect(CombatEntityStats caster, CombatEntityStats receiver, StatusEffectType type, int amount)
    {
        // 약화 등으로 밸류가 0이 되면 아무 효과 없음 (지속시간도 갱신하지 않음)
        if (amount <= 0) return;

        int added = receiver.AddStatus(type, amount);
        bool isBuff = CombatEntityStats.IsBuff(type);

        Debug.Log($"[{(isBuff ? "버프" : "디버프")}] {GetStatusName(type)} +{added} " +
                  $"(현재 {receiver.GetStatusValue(type)}, {receiver.GetStatusTurns(type)}턴)");

        // 플로팅 텍스트 (흡혈은 %로 표시)
        string suffix = type == StatusEffectType.Lifesteal ? "%" : string.Empty;
        Color color = isBuff ? new Color(1f, 0.85f, 0.2f) : new Color(0.75f, 0.35f, 1f);
        BattleUIManager.Instance?.ShowStatusText(GetEntityWorldPosition(receiver), $"{GetStatusName(type)} +{added}{suffix}", color);

        if (isBuff) BattleVFXPlayer.Instance?.Play(GetEntityTransform(receiver), VFXType.Buff);
        SoundManager.Instance.PlaySFX(SFXType.Buff);

        // 시전자 모션
        if (caster == playerStats) BattleUIManager.Instance.PlayPlayerAttack();
        else BattleUIManager.Instance.PlayMonsterAttack();
    }

    public static string GetStatusName(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Regeneration: return "재생";
            case StatusEffectType.Force:        return "힘";
            case StatusEffectType.Lifesteal:    return "흡혈";
            case StatusEffectType.Poison:       return "중독";
            case StatusEffectType.Bleed:        return "출혈";
            case StatusEffectType.Weaken:       return "약화";
            default:                            return type.ToString();
        }
    }

    private void HandleDeath(CombatEntityStats deadEntity)
    {
        if (isGameOver) return;     // 이미 승패가 결정됐다면 무시
        isGameOver = true;

        if (deadEntity == playerStats)
        {
            Debug.Log("[전투 종료] 플레이어 사망 - 패배");
            BattleUIManager.Instance?.DefeatUI();
            SoundManager.Instance.PlayBGM(BGMType.Defeat);
        }
        else
        {
            Debug.Log("[전투 종료] 몬스터 처치 - 승리");
            BattleUIManager.Instance?.VictoryUI();
            SoundManager.Instance.PlayBGM(BGMType.Victory);
            GameManager.Instance.currentHP = playerStats.currentHp;
        }
    }

    /// <summary>
    /// 턴 종료 시 방어도만 초기화 (버프/디버프는 유지 턴수로 관리)
    /// </summary>
    private void TurnResetGuard()
    {
        playerStats.guard = 0;
        monsterStats.guard = 0;
        BattleUIManager.Instance?.UpdateAllUI();
    }

    public void SetSlotActionDelay(float newDelay)
    {
        slotActionDelay = Mathf.Clamp(newDelay, 0.2f, 3.0f);
        Debug.Log($"[전투 매니저] 시전 딜레이 적용: {slotActionDelay:F2}초");
    }
}
