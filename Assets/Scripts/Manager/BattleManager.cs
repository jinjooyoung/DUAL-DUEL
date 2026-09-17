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

    [Header("카드 시전 딜레이")]
    [SerializeField] private float slotActionDelay = 1.0f;

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
        for (int i = 0; i < fieldCardSlots.Count; i++)
        {
            BattleSlot slot = fieldCardSlots[i];

            if (slot != null && slot.isOccupied && slot.currentCard != null)
            {
                ExecuteSlotAction(slot);
                yield return new WaitForSeconds(slotActionDelay);
            }
        }

        OnAllSlotsFinished();
        turnExecutionCoroutine = null;
    }

    private void ExecuteSlotAction(BattleSlot slot)
    {
        CardSO card = slot.currentCard.cardSO;
        if (card == null) return;

        bool isPlayerSlot = slot.slotOwnerType == OwnerType.Player;
        CombatEntityStats user = isPlayerSlot ? playerStats : monsterStats;
        CombatEntityStats target = isPlayerSlot ? monsterStats : playerStats;

        int cardValue = (card.values != null && card.values.Count > 0) ? card.values[0] : 0;

        switch (card.cardType)
        {
            case CardType.Attack:
                int finalDamage = Mathf.Max(0, cardValue + user.buffValue - user.debuffValue);
                ApplyDamage(target, finalDamage);
                break;

            case CardType.Defense:
                ModifyGuard(user, cardValue);
                break;

            case CardType.Buff:
                ModifyBuff(user, cardValue);
                break;

            case CardType.Debuff:
                ModifyDebuff(target, cardValue);
                break;
        }

        BattleCardManager.Instance?.DiscardCard(slot.currentCard);
        slot.ClearSlot();
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

    public void ApplyDamage(CombatEntityStats target, int damage)
    {
        if (damage <= 0) return;

        if (target.guard > 0)
        {
            if (target.guard >= damage)
            {
                target.guard -= damage;
                damage = 0;
            }
            else
            {
                damage -= target.guard;
                target.guard = 0;
            }
        }

        if (damage > 0)
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

    private void OnAllSlotsFinished()
    {
        Debug.Log("[턴 종료 연산 완료] 모든 슬롯 정산 끝");
    }
}
