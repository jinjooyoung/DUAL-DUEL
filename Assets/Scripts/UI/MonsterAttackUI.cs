using TMPro;
using UnityEngine;

/// <summary>
/// 이번 턴 몬스터 기본 공격력 표시 (몬스터 머리 위 등). 툴팁도 같이 제공
/// TooltipTrigger를 같은 오브젝트에 붙이면 마우스를 올렸을 때 설명이 뜸
/// </summary>
public class MonsterAttackUI : MonoBehaviour, ITooltipTextProvider
{
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private string tableName = "Card_Table";

    private bool isSubscribed;

    private void Start() => Subscribe();
    private void OnEnable() { if (BattleManager.Instance != null) Subscribe(); }

    private void Subscribe()
    {
        if (isSubscribed || BattleManager.Instance == null) return;
        BattleManager.Instance.OnMonsterTurnAttackChanged += Refresh;
        isSubscribed = true;
        Refresh(BattleManager.Instance.monsterTurnAttack);
    }

    private void OnDisable()
    {
        if (isSubscribed && BattleManager.Instance != null)
            BattleManager.Instance.OnMonsterTurnAttackChanged -= Refresh;
        isSubscribed = false;
    }

    private void Refresh(int attack)
    {
        if (attackText != null) attackText.text = attack.ToString();
    }

    public string GetTooltipText()
    {
        int attack = BattleManager.Instance != null ? BattleManager.Instance.monsterTurnAttack : 0;
        return LocalizedTextUtil.Get(tableName, "MONSTER_ATTACK_DESC",
            "<size=20>적의 공격</size>모든 카드 시전이 끝나면 적이 {0}만큼 공격합니다.", attack);
    }
}
