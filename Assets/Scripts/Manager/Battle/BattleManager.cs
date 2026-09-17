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
    [Header("전투 슬롯 필드")]
    public List<BattleSlot> fieldCardSlots = new List<BattleSlot>();

    [Header("전투 주체 스탯")]
    public CombatEntityStats playerStats;
    public CombatEntityStats monsterStats;

    [Header("몬스터 기본 액션")]
    [Tooltip("슬롯 외 몬스터의 턴 종료 확정 기본 공격력")]
    public int monsterBaseAttack = 5;

    [Header("카드 시전 딜레이")]
    [SerializeField] private float slotActionDelay = 1.0f;

    [Header("턴 전환 대기 시간")]
    [SerializeField] private float nextTurnDelay = 2.0f; // 턴 종료 후 다음 드로우까지 대기 시간 (2초)

    private Coroutine turnExecutionCoroutine;

    private void Start()
    {
        InitCombatStats();
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
        turnExecutionCoroutine = StartCoroutine(Co_ExecuteTurnSlots());
    }

    private IEnumerator Co_ExecuteTurnSlots()
    {
        // 1. 슬롯 5개 순차 발동 (1초 간격)
        for (int i = 0; i < fieldCardSlots.Count; i++)
        {
            BattleSlot slot = fieldCardSlots[i];

            if (slot != null && slot.isOccupied && slot.currentCard != null)
            {
                ExecuteSlotAction(slot);
                yield return new WaitForSeconds(slotActionDelay);
            }
        }

        // 1-1. 슬롯 5개 연산 후 몬스터가 살아있다면 확정 기본 공격 실행
        if (monsterStats.currentHp > 0)
        {
            yield return StartCoroutine(Co_ExecuteMonsterBaseAttack());
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
    /// 슬롯 외 몬스터의 확정 기본 공격 연출 및 데미지 연산 코루틴
    /// </summary>
    private IEnumerator Co_ExecuteMonsterBaseAttack()
    {
        if (monsterBaseAttack <= 0) yield break;

        Debug.Log($"[몬스터 기본 공격] 몬스터가 플레이어에게 기본 공격 {monsterBaseAttack} 시전!");

        // 몬스터에게 남은 버프/디버프가 있다면 가산 적용
        int finalDamage = Mathf.Max(0, monsterBaseAttack + monsterStats.buffValue - monsterStats.debuffValue);
        monsterStats.buffValue = 0;
        monsterStats.debuffValue = 0;

        // 플레이어에게 데미지 적용 (방어도 우선 차감 후 잔여 피해 체력 차감)
        ApplyDamage(playerStats, finalDamage);

        BattleUIManager.Instance?.UpdateAllUI();

        // 공격 액션 후 1초 연출 대기
        yield return new WaitForSeconds(slotActionDelay);
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
            StartCoroutine(BattleCardManager.Instance.Co_DrawCards(6));
        }

        BattleUIManager.Instance?.UpdateAllUI();
    }

    private void ExecuteSlotAction(BattleSlot slot)
    {
        CardSO card = slot.currentCard.cardSO;
        if (card == null) return;

        bool isPlayerSlot = slot.slotOwnerType == OwnerType.Player;
        CombatEntityStats user = isPlayerSlot ? playerStats : monsterStats;
        CombatEntityStats target = isPlayerSlot ? monsterStats : playerStats;

        // 1. 카드의 기본 밸류 확인
        int baseCardValue = (card.values != null && card.values.Count > 0) ? card.values[0] : 0;

        // 2. 주체의 버프/디버프 확인 및 최종 밸류 계산 (음수면 0으로 보정)
        int finalValue = Mathf.Max(0, baseCardValue + user.buffValue - user.debuffValue);

        // 3. 이번 행동에 반영했으므로 주체의 버프/디버프 소진 (0으로 초기화)
        user.buffValue = 0;
        user.debuffValue = 0;

        // 4. 최종 밸류를 바탕으로 카드 효과 실행 (0이면 실질적으로 효과 없음)
        switch (card.cardType)
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
