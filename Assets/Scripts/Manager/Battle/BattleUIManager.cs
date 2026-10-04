using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
        else Destroy(gameObject);

        InitFloatingTextPool();
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
}