using UnityEngine;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    [Header("텍스트 UI (TextMeshPro)")]
    [Tooltip("뽑을 카드 더미 카운트 텍스트")]
    [SerializeField] private TextMeshProUGUI drawDeckText;

    [Tooltip("버려진 카드 더미 카운트 텍스트")]
    [SerializeField] private TextMeshProUGUI discardDeckText;

    [Tooltip("플레이어 스탯 텍스트")]
    [SerializeField] private TextMeshProUGUI playerStatText;

    [Tooltip("몬스터 스탯 텍스트")]
    [SerializeField] private TextMeshProUGUI monsterStatText;

    [Tooltip("몬스터 이번 턴 공격력 텍스트")]
    [SerializeField] private TextMeshProUGUI monsterTurnDamageText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 모든 배틀 UI 텍스트 즉시 갱신
    /// </summary>
    public void UpdateAllUI()
    {
        UpdateDeckUI();
        UpdateCombatStatsUI();
        UpdateMonsterTurnDamage();
    }

    /// <summary>
    /// 드로우 덱 및 버림 덱 카운트 갱신
    /// </summary>
    public void UpdateDeckUI()
    {
        if (BattleCardManager.Instance == null) return;

        if (drawDeckText != null)
        {
            drawDeckText.text = $"드로우덱\n{BattleCardManager.Instance.drawDeck.Count}";
        }

        if (discardDeckText != null)
        {
            discardDeckText.text = $"버림 덱\n{BattleCardManager.Instance.discardDeck.Count}";
        }
    }

    /// <summary>
    /// 플레이어 및 몬스터 전투 스탯 텍스트 갱신
    /// </summary>
    public void UpdateCombatStatsUI()
    {
        if (BattleManager.Instance == null) return;

        // 플레이어 스탯 갱신
        if (playerStatText != null && BattleManager.Instance.playerStats != null)
        {
            playerStatText.text = $"플레이어 체력 : {BattleManager.Instance.playerStats.currentHp}\n플레이어 방어력 : {BattleManager.Instance.playerStats.guard}";
        }

        // 몬스터 스탯 갱신
        if (monsterStatText != null && BattleManager.Instance.monsterStats != null)
        {
            monsterStatText.text = $"몬스터 체력 : {BattleManager.Instance.monsterStats.currentHp}\n몬스터 방어력 : {BattleManager.Instance.monsterStats.guard}";
        }
    }

    public void UpdateMonsterTurnDamage()
    {
        if (monsterTurnDamageText != null)
            monsterTurnDamageText.text = $"이번 턴 적 공격 : {BattleManager.Instance.monsterBaseAttack}";
    }
}