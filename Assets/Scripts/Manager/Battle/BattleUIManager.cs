using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    [Header("텍스트 UI (TextMeshPro)")]
    [Tooltip("뽑을 카드 더미 카운트 텍스트")]
    [SerializeField] private TextMeshProUGUI drawDeckText;

    [Tooltip("버려진 카드 더미 카운트 텍스트")]
    [SerializeField] private TextMeshProUGUI discardDeckText;

    [Header("플레이어 스탯 UI")]
    [SerializeField] private TextMeshProUGUI playerStatText;
    [SerializeField] private Slider playerHpSlider;
    [SerializeField] private Slider playerShieldSlider;

    [Header("플레이어 표정")]
    [SerializeField] private CharacterExpression playerExpression;

    [Header("몬스터 스탯 UI")]
    [SerializeField] private TextMeshProUGUI monsterStatText;
    [SerializeField] private Slider monsterHpSlider;
    [SerializeField] private Slider monsterShieldSlider;

    [Header("몬스터 표정")]
    [SerializeField] private CharacterExpression monsterExpression;

    [Header("배틀 종료 패널")]
    public GameObject endOfBattlePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI reviewText;

    [Header("플로팅 텍스트 오브젝트")]
    [Tooltip("3D TextMeshPro가 붙은 플로팅 텍스트 프리팹")]
    [SerializeField] private GameObject floatingTextPrefab;
    [Tooltip("초기 생성 수량")]
    [SerializeField] private int initialPoolSize = 2;

    // 비활성화된 오브젝트들을 담아둘 풀 리스트
    public List<FloatingTextEffect> floatingTextPool = new List<FloatingTextEffect>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitFloatingTextPool();

        if (endOfBattlePanel != null)
            endOfBattlePanel.SetActive(false);
    }

    private void Start()
    {
        InitializeCombatStatsUI();
    }

    /// <summary>
    /// 게임 시작 시 초기 풀 생성
    /// </summary>
    private void InitFloatingTextPool()
    {
        if (floatingTextPrefab == null) return;

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewFloatingText();
        }
    }

    private void InitializeCombatStatsUI()
    {
        if (BattleManager.Instance == null) return;

        if (BattleManager.Instance.playerStats != null)
        {
            if (playerHpSlider != null) playerHpSlider.maxValue = GameManager.Instance.character.maxHp;
            if (playerShieldSlider != null) playerShieldSlider.maxValue = GameManager.Instance.character.maxHp;
        }

        if (BattleManager.Instance.monsterStats != null)
        {
            if (monsterHpSlider != null) monsterHpSlider.maxValue = BattleManager.Instance.monsterStats.currentHp;
            if (monsterShieldSlider != null) monsterShieldSlider.maxValue = BattleManager.Instance.monsterStats.currentHp;
        }

        UpdateCombatStatsUI();
    }

    /// <summary>
    /// 동적 확장용 인스턴스화
    /// </summary>
    private FloatingTextEffect CreateNewFloatingText()
    {
        GameObject obj = Instantiate(floatingTextPrefab, transform);
        obj.SetActive(false);

        FloatingTextEffect effect = obj.GetComponent<FloatingTextEffect>();
        if (effect == null) effect = obj.AddComponent<FloatingTextEffect>();

        floatingTextPool.Add(effect);
        return effect;
    }

    /// <summary>
    /// 풀에서 비활성화된 텍스트를 가져오거나 부족하면 새로 생성 (동적 확장)
    /// </summary>
    private FloatingTextEffect GetOrCreateFloatingText()
    {
        // 1. 꺼져 있는 오브젝트 탐색
        FloatingTextEffect available = floatingTextPool.Find(t => t != null && !t.gameObject.activeSelf);
        if (available != null) return available;

        // 2. 모자라면 확장 생성
        return CreateNewFloatingText();
    }

    #region 플로팅 텍스트 호출 API

    /// <summary>
    /// 데미지 플로팅 텍스트 (빨간색)
    /// </summary>
    public void ShowDamageText(Vector3 worldPos, int damage)
    {
        Vector3 randomPos = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.5f), -0.05f);
        FloatingTextEffect effect = GetOrCreateFloatingText();
        if (effect != null)
        {
            effect.Play($"-{damage}", new Color(1f, 0.25f, 0.2f), randomPos, Vector3.up, 0.45f, 0.6f);
        }
    }

    /// <summary>
    /// 힐 플로팅 텍스트 (초록색)
    /// </summary>
    public void ShowHealText(Vector3 worldPos, int heal)
    {
        Vector3 randomPos = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.5f), -0.05f);
        FloatingTextEffect effect = GetOrCreateFloatingText();
        if (effect != null)
        {
            effect.Play($"+{heal}", new Color(0.2f, 1f, 0.35f), randomPos, Vector3.up, 0.45f, 0.6f);
        }
    }

    /// <summary>
    /// 방어도/버프 플로팅 텍스트 (하늘색/노란색)
    /// </summary>
    public void ShowShieldText(Vector3 worldPos, int shield)
    {
        Vector3 randomPos = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.5f), -0.05f);
        FloatingTextEffect effect = GetOrCreateFloatingText();
        if (effect != null)
        {
            effect.Play($"+{shield} 방어", new Color(0.3f, 0.7f, 1f), randomPos, Vector3.up, 0.4f, 0.6f);
        }
    }

    #endregion

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
        if (BattleCardManager.Instance == null) return;

        if (drawDeckText != null)
        {
            drawDeckText.text = $"{BattleCardManager.Instance.drawDeck.Count}";
        }

        if (discardDeckText != null)
        {
            discardDeckText.text = $"{BattleCardManager.Instance.discardDeck.Count}";
        }
    }

    /// <summary>
    /// 플레이어 및 몬스터 전투 스탯 텍스트 갱신
    /// </summary>
    public void UpdateCombatStatsUI()
    {
        if (BattleManager.Instance == null) return;

        // 플레이어 스탯
        if (BattleManager.Instance.playerStats != null)
        {
            var stats = BattleManager.Instance.playerStats;

            if (playerStatText != null)
            {
                playerStatText.text =
                    $"Player\n<size=36>{stats.currentHp}/{playerHpSlider.maxValue}    +{stats.guard}</size>";
            }

            if (playerHpSlider != null)
                playerHpSlider.value = stats.currentHp;

            if (playerShieldSlider != null)
                playerShieldSlider.value = stats.guard;
        }

        // 몬스터 스탯
        if (BattleManager.Instance.monsterStats != null)
        {
            var stats = BattleManager.Instance.monsterStats;

            if (monsterStatText != null)
            {
                monsterStatText.text =
                    $"Monster\n<size=36>{stats.currentHp}/{monsterHpSlider.maxValue}    +{stats.guard}</size>";
            }

            if (monsterHpSlider != null)
                monsterHpSlider.value = stats.currentHp;

            if (monsterShieldSlider != null)
                monsterShieldSlider.value = stats.guard;
        }
    }

    public void PlayPlayerHit() => playerExpression.PlayHit();
    public void PlayPlayerAttack() => playerExpression.PlayAttack();
    public void PlayMonsterHit() => monsterExpression.PlayHit();
    public void PlayMonsterAttack() => monsterExpression.PlayAttack();

    public void VictoryUI()
    {
        titleText.text = "승리!";
        reviewText.text = "";

        OnEndOfBattlePanel();
    }

    public void DefeatUI()
    {
        titleText.text = "패배!";
        reviewText.text = "";

        OnEndOfBattlePanel();
    }

    public void rewardPanel()
    {
        RewardCardUI rewardUI = FindFirstObjectByType<RewardCardUI>(FindObjectsInactive.Include);
        if (rewardUI != null)
        {
            OnEndOfBattlePanel();
            rewardUI.OpenRewardPanel();
        }
    }

    public void OnEndOfBattlePanel()
    {
        if (endOfBattlePanel != null) endOfBattlePanel.SetActive(true);
    }
}