using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CombatEntityStats
{
    [Tooltip("현재 체력")] public int currentHp;
    [Tooltip("최대 체력")] public int maxHp;
    [Tooltip("현재 방어도")] public int guard;
    [Tooltip("버프 밸류 (공격력/위력 가산 등)")] public int buffValue;
    [Tooltip("디버프 밸류 (약화 등)")] public int debuffValue;

    public CombatEntityStats(int maxHp)
    {
        this.maxHp = maxHp;
        this.currentHp = maxHp;
        this.guard = 0;
        this.buffValue = 0;
        this.debuffValue = 0;
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

    [Header("카드 시전 딜레이")]
    [SerializeField] private float slotActionDelay = 0.5f;

    [Header("턴 전환 대기 시간")]
    [SerializeField] private float nextTurnDelay = 2.0f; // 턴 종료 후 다음 드로우까지 대기 시간 (2초)

    private Coroutine turnExecutionCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 순서 보장을 위해 초기화 로직 모음
    private void Start()
    {
        InitCombatStats();
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
            int pMaxHp = GameManager.Instance.character != null ? GameManager.Instance.character.maxHp : 50;
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
        // 1. 슬롯 5개 순차 발동 (1초 간격)
        for (int i = 0; i < fieldCardSlots.Count; i++)
        {
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
            if (monsterStats.currentHp <= 0) break;
        }

        // 2. 슬롯 발동 완료 후, 손패에 남아있는 잉여 카드들 0.5초 간격으로 모두 버림
        if (BattleCardManager.Instance != null)
        {
            yield return StartCoroutine(BattleCardManager.Instance.Co_DiscardAllHandCards());
        }

        TurnResetGuardBuff();

        // 3. 손패 정리 완료 후 2초 대기 (턴 전환 딜레이 연출)
        yield return new WaitForSeconds(nextTurnDelay);

        // 4. 모든 정산 완료 및 새 턴 시작
        OnAllSlotsFinished();
        turnExecutionCoroutine = null;
    }

    /// <summary>
    /// 카드가 비어 있는 슬롯에서 슬롯 고유의 기본 공격력을 시전하는 코루틴
    /// </summary>
    private IEnumerator Co_ExecuteSlotBaseAttack(BattleSlot slot)
    {
        bool isPlayerSlot = slot.slotOwnerType == OwnerType.Player;
        CombatEntityStats user = isPlayerSlot ? playerStats : monsterStats;
        CombatEntityStats target = isPlayerSlot ? monsterStats : playerStats;

        int rawAtk = slot.slotBaseAttack;

        // 주체의 버프/디버프 가산 적용
        int finalDamage = Mathf.Max(0, rawAtk + user.buffValue - user.debuffValue);
        user.buffValue = 0;
        user.debuffValue = 0;

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
    /// </summary>
    private bool CheckCardCondition(BattleSlot slot, CardSO cardData)
    {
        bool isConditionMet = false;
        int currentIdx = slot.slotIndex;
        string param = cardData.conditionParam;

        switch (cardData.conditionCategory)
        {
            // 1. 조건 없음: 항상 참
            case ConditionCategory.None:
                isConditionMet = true;
                break;

            // 2. 슬롯 번호 제한 ("0,4" 또는 "1" 등)
            case ConditionCategory.SlotIndexRestriction:
                if (!string.IsNullOrEmpty(param))
                {
                    string[] targetIndices = param.Split(',');
                    foreach (string idxStr in targetIndices)
                    {
                        if (int.TryParse(idxStr.Trim(), out int targetIdx))
                        {
                            if (currentIdx == targetIdx)
                            {
                                isConditionMet = true;
                                break;
                            }
                        }
                    }
                }
                break;

            // 3. 인접 슬롯 카드 타입 검사 (0번/4번 제외, 1~3번 슬롯의 앞뒤에 카드가 모두 있고 둘 다 타입 일치)
            case ConditionCategory.NeighborCardType:
                if (currentIdx > 0 && currentIdx < fieldCardSlots.Count - 1)
                {
                    BattleSlot prevSlot = fieldCardSlots[currentIdx - 1];
                    BattleSlot nextSlot = fieldCardSlots[currentIdx + 1];

                    // 앞뒤 슬롯에 모두 카드가 배치되어 있는지 검사
                    if (prevSlot != null && prevSlot.isOccupied && prevSlot.currentCard != null &&
                        nextSlot != null && nextSlot.isOccupied && nextSlot.currentCard != null)
                    {
                        if (Enum.TryParse(param.Trim(), true, out CardType requiredType))
                        {
                            CardType prevType = prevSlot.currentCard.cardInstance.baseData.cardType;
                            CardType nextType = nextSlot.currentCard.cardInstance.baseData.cardType;

                            if (prevType == requiredType && nextType == requiredType)
                            {
                                isConditionMet = true;
                            }
                        }
                    }
                }
                break;

            // 4. 직전 슬롯 소유자 검사 (0번 슬롯 제외, 1~4번의 직전 슬롯 OwnerType 일치)
            case ConditionCategory.PrevSlotOwner:
                if (currentIdx > 0 && currentIdx < fieldCardSlots.Count)
                {
                    BattleSlot prevSlot = fieldCardSlots[currentIdx - 1];
                    if (prevSlot != null)
                    {
                        if (Enum.TryParse(param.Trim(), true, out OwnerType targetOwner))
                        {
                            if (prevSlot.slotOwnerType == targetOwner)
                            {
                                isConditionMet = true;
                            }
                        }
                    }
                }
                break;

            // 5. 손패 카드 수량 검사 (예: "Count>=5", "Count<=2")
            case ConditionCategory.HandCount:
                if (BattleCardManager.Instance != null)
                {
                    int currentHandCount = BattleCardManager.Instance.handCards.Count;
                    // "Count" 접두사 제거 후 부등호 연산 ("Count>=5" -> ">=5")
                    string expression = param.Replace("Count", "").Trim();
                    isConditionMet = EvaluateComparison(currentHandCount, expression);
                }
                break;

            // 6. 손패 특정 타입 매수 검사 (예: "Attack>=4", "Defense>=2")
            case ConditionCategory.HandType:
                if (BattleCardManager.Instance != null)
                {
                    // 타입명과 부등호 수식 분리 (예: "Attack"과 ">=4")
                    if (TryParseHandTypeExpression(param, out CardType targetType, out string expression))
                    {
                        int matchedTypeCount = 0;
                        foreach (var handCard in BattleCardManager.Instance.handCards)
                        {
                            if (handCard != null && handCard.baseData != null && handCard.baseData.cardType == targetType)
                            {
                                matchedTypeCount++;
                            }
                        }

                        isConditionMet = EvaluateComparison(matchedTypeCount, expression);
                    }
                }
                break;
        }

        return isConditionMet;
    }

    /// <summary>
    /// 한 턴의 모든 연출과 버리기가 끝나고 새 턴이 시작될 때 호출
    /// </summary>
    private void OnAllSlotsFinished()
    {
        Debug.Log("[새 턴 시작] 다음 턴 슬롯 재배정 및 6장 드로우 시작");

        // 다음 턴 슬롯 5개 타입(아군/적) 새로 배정
        BattleSlotManager.Instance?.GenerateTurnSlotTypes();

        // 2) 0.5초 간격으로 6장 다시 드로우 시작
        if (BattleCardManager.Instance != null)
        {
            StartCoroutine(BattleCardManager.Instance.Co_DrawCards(BattleCardManager.Instance.drawCount));
        }

        BattleUIManager.Instance?.UpdateAllUI();
    }

    private IEnumerator ExecuteSlotAction(BattleSlot slot)
    {
        CardInstance instance = slot.currentCard.cardInstance;
        if (instance == null || instance.baseData == null) yield break;

        CardSO cardData = instance.baseData;

        bool isPlayerSlot = slot.slotOwnerType == OwnerType.Player;
        CombatEntityStats user = isPlayerSlot ? playerStats : monsterStats;
        CombatEntityStats target = isPlayerSlot ? monsterStats : playerStats;

        // 0. 조건 만족 여부 검사
        bool isConditionMet = CheckCardCondition(slot, cardData);
        Debug.Log($"[{slot.slotIndex}번 슬롯] {cardData.nameKey} 조건({cardData.conditionCategory}): {(isConditionMet ? "달성!" : "미달성")}");

        // 1. 카드의 밸류 확인(강화 단계가 적용된 GetValue() 호출)
        int baseCardValue = instance.GetValue();

        // 2. 주체의 버프/디버프 확인 및 최종 밸류 계산 (음수면 0으로 보정)
        int finalValue = Mathf.Max(0, baseCardValue + user.buffValue - user.debuffValue);

        // 3. 이번 행동에 반영했으므로 주체의 버프/디버프 소진 (0으로 초기화)
        user.buffValue = 0;
        user.debuffValue = 0;

        // 4. 최종 밸류를 바탕으로 카드 효과 실행 (0이면 실질적으로 효과 없음)
        switch (cardData.cardType)
        {
            case CardType.Attack:
                ApplyDamage(target, finalValue);
                break;

            case CardType.Defense:
                ModifyGuard(user, finalValue);
                break;

            case CardType.Heal:
                ModifyHealth(user, finalValue);
                break;

            case CardType.Buff:
                ModifyBuff(user, finalValue);
                break;

            case CardType.Debuff:
                ModifyDebuff(target, finalValue);
                break;
        }

        // 사용 연출
        Sequence sequence = DOTweenManager.CardExecute(
            slot.currentCard.transform,
            isPlayerSlot ? Vector3.right : Vector3.left
        );

        // 연출이 끝날 때까지 대기
        yield return sequence.WaitForCompletion();

        // 5. 카드 회수 및 슬롯 정리, UI 갱신
        BattleCardManager.Instance?.DiscardCard(slot.currentCard);
        slot.ClearSlot();
        BattleUIManager.Instance?.UpdateAllUI();
    }

    public void ModifyHealth(CombatEntityStats entity, int amount)
    {
        entity.currentHp = Mathf.Clamp(entity.currentHp + amount, 0, entity.maxHp);
        Debug.Log($"HP 변경: {amount} (현재 HP: {entity.currentHp}/{entity.maxHp})");

        if (entity.currentHp <= 0)
        {
            HandleDeath(entity);
        }
    }

    /// <summary>
    /// 공격 데미지 처리 (방어도를 먼저 소진하고, 초과분만 체력에서 차감)
    /// </summary>
    public void ApplyDamage(CombatEntityStats target, int damage)
    {
        if (damage <= 0) return;

        // 1. 방어도가 존재하는 경우
        if (target.guard > 0)
        {
            // 공격력이 방어도보다 큰 경우 (방어도 전량 파괴 + 잔여 피해 체력 차감)
            if (damage > target.guard)
            {
                int remainingDamage = damage - target.guard;
                target.guard = 0;
                ModifyHealth(target, -remainingDamage);
            }
            // 방어도가 공격력 이상인 경우 (방어도만 공격력만큼 차감되고 체력 피해 없음)
            else
            {
                target.guard -= damage;
            }
        }
        // 2. 방어도가 0인 경우 (데미지 전량 체력 차감)
        else
        {
            ModifyHealth(target, -damage);
        }
    }

    public void ModifyGuard(CombatEntityStats entity, int amount)
    {
        entity.guard = Mathf.Max(0, entity.guard + amount);
        Debug.Log($"방어도 변경: {amount} (현재 방어도: {entity.guard})");
    }

    public void ModifyBuff(CombatEntityStats entity, int amount)
    {
        entity.buffValue = Mathf.Max(0, entity.buffValue + amount);
        Debug.Log($"버프 변경: {amount} (현재 버프: {entity.buffValue})");
    }

    public void ModifyDebuff(CombatEntityStats entity, int amount)
    {
        entity.debuffValue = Mathf.Max(0, entity.debuffValue + amount);
        Debug.Log($"디버프 변경: {amount} (현재 디버프: {entity.debuffValue})");
    }

    private void HandleDeath(CombatEntityStats deadEntity)
    {
        if (deadEntity == playerStats)
        {
            Debug.Log("[전투 종료] 플레이어 사망 - 패배");
        }
        else
        {
            Debug.Log("[전투 종료] 몬스터 처치 - 승리");
        }
    }

    /// <summary>
    /// ">=5", "<=3", "==2" 등의 문자열 부등호 수식을 파싱하여 실제 값과 비교
    /// </summary>
    private bool EvaluateComparison(int actualValue, string expression)
    {
        expression = expression.Trim();

        if (expression.StartsWith(">="))
        {
            if (int.TryParse(expression.Substring(2).Trim(), out int target))
                return actualValue >= target;
        }
        else if (expression.StartsWith("<="))
        {
            if (int.TryParse(expression.Substring(2).Trim(), out int target))
                return actualValue <= target;
        }
        else if (expression.StartsWith(">"))
        {
            if (int.TryParse(expression.Substring(1).Trim(), out int target))
                return actualValue > target;
        }
        else if (expression.StartsWith("<"))
        {
            if (int.TryParse(expression.Substring(1).Trim(), out int target))
                return actualValue < target;
        }
        else if (expression.StartsWith("=="))
        {
            if (int.TryParse(expression.Substring(2).Trim(), out int target))
                return actualValue == target;
        }
        else
        {
            // 부등호 없이 숫자만 적힌 경우 기본 >= 로 판정
            if (int.TryParse(expression, out int target))
                return actualValue >= target;
        }

        return false;
    }

    /// <summary>
    /// "Attack>=4" 형태의 문자열에서 ">=4" 수식을 분리
    /// </summary>
    private bool TryParseHandTypeExpression(string rawParam, out CardType type, out string expression)
    {
        type = CardType.Attack;
        expression = string.Empty;

        if (string.IsNullOrEmpty(rawParam)) return false;

        // 부등호 기호 시작 위치 탐색
        int opIndex = rawParam.IndexOfAny(new char[] { '>', '<', '=' });
        if (opIndex <= 0) return false;

        string typeString = rawParam.Substring(0, opIndex).Trim();
        expression = rawParam.Substring(opIndex).Trim();

        return Enum.TryParse(typeString, true, out type);
    }

    private void TurnResetGuardBuff()
    {
        playerStats.guard = 0;
        playerStats.buffValue = 0;
        playerStats.debuffValue = 0;
        monsterStats.guard = 0;
        monsterStats.buffValue = 0;
        monsterStats.debuffValue = 0;
        BattleUIManager.Instance?.UpdateAllUI();
    }
}
