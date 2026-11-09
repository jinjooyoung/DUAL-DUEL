using UnityEngine;

/// <summary>
/// 버프/디버프 아이콘에 붙여서, 마우스를 올린 순간 BattleManager의 현재 수치와 남은 턴으로 툴팁 문자열을 만들어 TooltipTrigger에 전달
/// 같은 오브젝트에 TooltipTrigger + BuffIconUI와 함께 부착
/// </summary>
[RequireComponent(typeof(TooltipTrigger))]
public class BuffTooltipBinder : MonoBehaviour, ITooltipTextProvider
{
    public enum TargetEntity
    {
        Player,
        Monster
    }

    [Header("이 아이콘이 표시하는 대상과 버프 종류")]
    [SerializeField] private TargetEntity targetEntity = TargetEntity.Player;
    [Tooltip("BattleUIManager의 아이콘 리스트 순서와 같게 설정 (0 재생, 1 힘, 2 흡혈, 3 중독, 4 출혈, 5 약화)")]
    [SerializeField] private StatusEffectType statusType;

    [Header("로컬라이제이션")]
    [Tooltip("키가 테이블에 없으면 아래 switch의 한국어 기본 문구를 사용")]
    [SerializeField] private string tableName = "Card_Table";

    public string GetTooltipText()
    {
        if (BattleManager.Instance == null) return string.Empty;

        CombatEntityStats stats = targetEntity == TargetEntity.Player
            ? BattleManager.Instance.playerStats
            : BattleManager.Instance.monsterStats;
        if (stats == null) return string.Empty;

        int value = stats.GetStatusValue(statusType);
        int turns = stats.GetStatusTurns(statusType);

        // {0} = 남은 턴, {1} = 수치
        switch (statusType)
        {
            case StatusEffectType.Regeneration:
                return LocalizedTextUtil.Get(tableName, "BUFFDESC_REGENERATION",
                    "<size=20>재생</size>{0}턴 동안 턴 종료 시 {1}만큼 회복합니다.", turns, value);

            case StatusEffectType.Force:
                return LocalizedTextUtil.Get(tableName, "BUFFDESC_FORCE",
                    "<size=20>힘</size>{0}턴 동안 공격 피해량이 {1} 증가합니다.", turns, value);

            case StatusEffectType.Lifesteal:
                return LocalizedTextUtil.Get(tableName, "BUFFDESC_LIFESTEAL",
                    "<size=20>흡혈</size>{0}턴 동안 체력에 입힌 피해의 {1}%만큼 회복합니다.", turns, value);

            case StatusEffectType.Poison:
                return LocalizedTextUtil.Get(tableName, "DEBUFFDESC_POISON",
                    "<size=20>중독</size>{0}턴 동안 턴 종료 시 {1}만큼 피해를 받습니다.", turns, value);

            case StatusEffectType.Bleed:
                return LocalizedTextUtil.Get(tableName, "DEBUFFDESC_BLEED",
                    "<size=20>출혈</size>{0}턴 동안 공격받을 때 {1}만큼 피해를 더 받습니다.", turns, value);

            case StatusEffectType.Weaken:
                return LocalizedTextUtil.Get(tableName, "DEBUFFDESC_WEAKEN",
                    "<size=20>약화</size>{0}턴 동안 카드 밸류가 {1}만큼 감소합니다.", turns, value);

            default:
                return string.Empty;
        }
    }
}
