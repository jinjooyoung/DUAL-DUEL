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

    [Header("플레이어 표정")]
    [SerializeField] private CharacterExpression playerExpression;

    [Header("몬스터 스탯 UI")]
    [SerializeField] private TextMeshProUGUI monsterStatText;
    [SerializeField] private Slider monsterHpSlider;

    [Header("몬스터 표정")]
    [SerializeField] private CharacterExpression monsterExpression;

    [Header("타입 제한 시 띄울 아이콘")]
    [SerializeField] private Image typeIcon;

    [Header("버프/디버프 아이콘 (순서 고정: 재생, 힘, 흡혈, 중독, 출혈, 약화)")]
    [SerializeField] private List<BuffIconUI> playerBuffIcons = new List<BuffIconUI>();
    [SerializeField] private List<BuffIconUI> monsterBuffIcons = new List<BuffIconUI>();

    [Header("배틀 종료 패널")]
    public GameObject endOfWinBattlePanel;
    public TextMeshProUGUI winReviewText;
    public GameObject endOfDefBattlePanel;
    public TextMeshProUGUI defReviewText;

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

        if (endOfWinBattlePanel != null)
            endOfWinBattlePanel.SetActive(false);
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
        }

        if (BattleManager.Instance.monsterStats != null)
        {
            if (monsterHpSlider != null) monsterHpSlider.maxValue = BattleManager.Instance.monsterStats.currentHp;
        }

        if (GameManager.Instance.monster.gimmickType1 == GimmickType.RestrictCardType) RestrictCardTypeMonsterUI(1);
        else if (GameManager.Instance.monster.gimmickType2 == GimmickType.RestrictCardType) RestrictCardTypeMonsterUI(2);

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
    /// 데미지 플로팅 텍스트
    /// </summary>
    public void ShowDamageText(Vector3 worldPos, int damage)
    {
        Vector3 randomPos = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.5f), -0.05f);
        FloatingTextEffect effect = GetOrCreateFloatingText();
        if (effect != null)
        {
            effect.PlayDamage(damage, randomPos, Vector3.up, 0.5f, 0.6f);
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
    /// 방어도 플로팅 텍스트 (하늘색)
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

    /// <summary>
    /// 버프/디버프 부여 플로팅 텍스트 (예: "힘 +2", "흡혈 +10%")
    /// </summary>
    public void ShowStatusText(Vector3 worldPos, string text, Color color)
    {
        Vector3 randomPos = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.2f, 0.5f), -0.05f);
        FloatingTextEffect effect = GetOrCreateFloatingText();
        if (effect != null)
        {
            effect.Play(text, color, randomPos, Vector3.up, 0.4f, 0.6f);
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
                    $"HP  {stats.currentHp}/{playerHpSlider.maxValue} +{stats.guard}";
            }

            if (playerHpSlider != null)
                playerHpSlider.value = stats.currentHp;

            UpdateBuffIcons(stats, playerBuffIcons);
        }

        // 몬스터 스탯
        if (BattleManager.Instance.monsterStats != null)
        {
            var stats = BattleManager.Instance.monsterStats;

            if (monsterStatText != null)
            {
                monsterStatText.text =
                    $"HP  {stats.currentHp}/{monsterHpSlider.maxValue} +{stats.guard}";
            }

            if (monsterHpSlider != null)
                monsterHpSlider.value = stats.currentHp;

            UpdateBuffIcons(stats, monsterBuffIcons);
        }
    }

    /// <summary>
    /// 엔티티의 버프/디버프 수치에 맞춰 아이콘 6개 on/off + 수치 갱신
    /// 리스트 인덱스 = StatusEffectType 정수값
    /// </summary>
    private void UpdateBuffIcons(CombatEntityStats stats, List<BuffIconUI> icons)
    {
        if (icons == null) return;

        int count = Mathf.Min(icons.Count, CombatEntityStats.STATUS_COUNT);
        for (int i = 0; i < count; i++)
        {
            if (icons[i] == null) continue;

            StatusEffectType type = (StatusEffectType)i;
            icons[i].Refresh(stats.GetStatusValue(type), type == StatusEffectType.Lifesteal);
        }
    }

    public void PlayPlayerHit() => playerExpression.PlayHit();
    public void PlayPlayerAttack() => playerExpression.PlayAttack();
    public void PlayMonsterHit() => monsterExpression.PlayHit();
    public void PlayMonsterAttack() => monsterExpression.PlayAttack();

    public void VictoryUI()
    {
        OnWinEndOfBattlePanel();
    }

    public void DefeatUI()
    {
        OnDefEndOfBattlePanel();
    }

    public void rewardPanel()
    {
        RewardCardUI rewardUI = FindFirstObjectByType<RewardCardUI>(FindObjectsInactive.Include);
        if (rewardUI != null)
        {
            OnWinEndOfBattlePanel();
            rewardUI.OpenRewardPanel();
        }
    }

    public void OnWinEndOfBattlePanel()
    {
        if (endOfWinBattlePanel != null)
        {
            if (GameManager.Instance.monster.monsterType == StageType.Boss)
            {
                winReviewText.text = $"플레이해 주셔서 감사합니다!\n현재 공개된 데모 버전은 1챕터까지입니다.\n[ESC] 설정창에서 간단한 플레이 후기를 남겨주시면\n더 완성도 높은 정식 버전을 만드는 데 큰 힘이 됩니다!";
                endOfWinBattlePanel.SetActive(true);

                return;
            }

            winReviewText.text = $"{BattleManager.Instance.CurrentMonster.rewardGold} 골드 획득!";
            endOfWinBattlePanel.SetActive(true);
        }
    }

    public void OnDefEndOfBattlePanel()
    {
        if (endOfDefBattlePanel != null)
        {
            int deckCount = GameManager.Instance.deck.Count;
            int gold = GameManager.Instance.currentGold;
            int chapter = StageManager.Instance.currentChapter;
            int floor = StageManager.Instance.currentNode.floor;

            defReviewText.text = $"보유한 카드 : {deckCount}\n보유한 골드 : {gold}\n도달한 층 : {chapter}챕터 {floor}층";
            endOfDefBattlePanel.SetActive(true);
        }
    }

    public void RestrictCardTypeMonsterUI(int param)
    {
        CardType type = param == 1 ? (CardType)GameManager.Instance.monster.gimmickParam1 : (CardType)GameManager.Instance.monster.gimmickParam2;
        string resourcePath = $"Cards/Public/{type}";

        typeIcon.gameObject.SetActive(true);
        typeIcon.sprite = Resources.Load<Sprite>(resourcePath);
    }
}