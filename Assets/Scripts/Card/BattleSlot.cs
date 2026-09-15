using UnityEngine;

public class BattleSlot : MonoBehaviour
{
    public bool IsPlayerType = true;
    public int slotIndex;           // 0 ~ 4 (좌측부터 순서대로)
    public bool isOccupied = false; // 카드가 이미 배치되어 있는지 여부
    public CardDisplay currentCard; // 배치된 카드 참조

    // 카드 배치 처리
    public void PlaceCard(CardDisplay card)
    {
        isOccupied = true;
        currentCard = card;
        card.transform.position = transform.position; // 슬롯 위치로 1회 고정
    }

    // 카드 회수/제거 시
    public void ClearSlot()
    {
        isOccupied = false;
        currentCard = null;
    }
}