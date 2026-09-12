using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("전투 슬롯 필드")]
    public List<BattleSlot> fieldCardSlots = new List<BattleSlot>();

    // 턴 종료
    public void TurnEnd()
    {
        foreach (var slot in fieldCardSlots)
        {
            Debug.Log($"슬롯_{slot.slotIndex}\n슬롯 타입: {slot.slotOwnerType}\n\n배치된 카드\n" +
                $"카드 이름: {slot.currentCard}\n카드 타입: {slot.currentCard.cardSO.ownerType}, {slot.currentCard.cardSO.cardType}\n" +
                $"카드 랭크: {slot.currentCard.cardSO.rank}\n카드 네임키: {slot.currentCard.cardSO.nameKey}\n카드 설명: {slot.currentCard.cardSO.descKey}\n" +
                $"카드 밸류: {slot.currentCard.cardSO.values}");
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
