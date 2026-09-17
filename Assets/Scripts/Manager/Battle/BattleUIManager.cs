using UnityEngine;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    [Header("전투 매니저 참조")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleCardManager cardManager;

    [Header("텍스트 UI (TextMeshPro)")]
    [Tooltip("뽑을 카드 더미 카운트 텍스트")]
    [SerializeField] private TextMeshProUGUI drawDeckText;

    [Tooltip("버려진 카드 더미 카운트 텍스트")]
    [SerializeField] private TextMeshProUGUI discardDeckText;

    [Tooltip("플레이어 스탯 텍스트")]
    [SerializeField] private TextMeshProUGUI playerStatText;

    [Tooltip("몬스터 스탯 텍스트")]
    [SerializeField] private TextMeshProUGUI monsterStatText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 매니저 참조가 비어있다면 자동 탐색
        if (battleManager == null) battleManager = FindAnyObjectByType<BattleManager>();
        if (cardManager == null) cardManager = FindAnyObjectByType<BattleCardManager>();
    }

    private void Start()
    {
        UpdateAllUI();
    }

    /// <summary>
    /// 모든 배틀 UI 텍스트 즉시 갱신
    /// </summary>
    public void UpdateAllUI()
    {
        UpdateDeckUI();
        UpdateCombatStatsUI();
    }

    /// <summary>
    /// 드로우 덱 및 버림 덱 카운트 갱신
    /// </summary>
    public void UpdateDeckUI()
    {
        if (cardManager == null) return;

        if (drawDeckText != null)
        {
            drawDeckText.text = $"드로우덱\n{cardManager.drawDeck.Count}";
        }

        if (discardDeckText != null)
        {
            discardDeckText.text = $"버림 덱\n{cardManager.discardDeck.Count}";
        }
    }

    /// <summary>
    /// 플레이어 및 몬스터 전투 스탯 텍스트 갱신
    /// </summary>
    public void UpdateCombatStatsUI()
    {
        if (battleManager == null) return;

        // 플레이어 스탯 갱신
        if (playerStatText != null && battleManager.playerStats != null)
        {
            playerStatText.text = $"플레이어 체력 : {battleManager.playerStats.currentHp}\n플레이어 방어력 : {battleManager.playerStats.guard}";
        }

        // 몬스터 스탯 갱신
        if (monsterStatText != null && battleManager.monsterStats != null)
        {
            monsterStatText.text = $"몬스터 체력 : {battleManager.monsterStats.currentHp}\n몬스터 방어력 : {battleManager.monsterStats.guard}";
        }
    }
}