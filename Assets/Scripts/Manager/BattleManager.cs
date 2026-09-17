using System.Collections.Generic;
using UnityEngine;

// 배틀 시 핸드 덱을 배치 및 사용하는 배틀의 큰 흐름을 관리하는 매니저
public class BattleManager : MonoBehaviour
{
    [Header("전투 슬롯 필드")]
    public List<BattleSlot> fieldCardSlots = new List<BattleSlot>();

    [Header("현재 전투 스탯")]
    [Tooltip("캐릭터 HP")]
    public int characterHP;
    [Tooltip("캐릭터 방어도")]
    public int characterGuard;
    [Tooltip("몬스터 HP")]
    public int monsterHP;
    [Tooltip("몬스터 방어도")]
    public int monsterGuard;

    private void Start()
    {
        // 전투 시작시 현재 캐릭터와 몬스터 HP 받아옴
        characterHP = GameManager.Instance.character.maxHp;
        monsterHP = GameManager.Instance.monster.maxHp;
    }

    // 턴 종료
    public void TurnEnd()
    {
        foreach (var slot in fieldCardSlots)
        {
            Debug.Log($"슬롯_{slot.slotIndex}\n슬롯 타입: {slot.slotOwnerType}\n\n배치된 카드\n" +
                $"카드 이름: {slot.currentCard}\n카드 타입: {slot.currentCard.cardSO.cardType}\n" +
                $"카드 랭크: {slot.currentCard.cardSO.rank}\n카드 네임키: {slot.currentCard.cardSO.nameKey}\n카드 설명: {slot.currentCard.cardSO.descKey}\n" +
                $"카드 밸류: {slot.currentCard.cardSO.values}");
        }
    }
}
