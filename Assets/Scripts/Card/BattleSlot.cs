using TMPro;
using UnityEngine;

public class BattleSlot : MonoBehaviour
{
    public OwnerType slotOwnerType;
    public int slotIndex;           // 0 ~ 4 (좌측부터 순서대로)
    public bool isOccupied = false; // 카드가 이미 배치되어 있는지 여부
    public CardDisplay currentCard; // 배치된 카드 참조

    [Header("슬롯 기본 공격력")]
    public int slotBaseAttack;      // 카드가 없을 때 발동할 공격력

    [Header("UI 바인딩")]
    [SerializeField] private TextMeshPro baseAtkText; // 인스펙터에서 직접 할당할 TMP

    // 카드 배치 처리
    public void PlaceCard(CardDisplay card)
    {
        isOccupied = true;
        currentCard = card;
        card.transform.position = transform.position; // 슬롯 위치로 1회 고정

        // 카드가 슬롯을 덮었으므로 슬롯 기본 공격력 텍스트 숨김
        if (baseAtkText != null)
        {
            baseAtkText.gameObject.SetActive(false);
        }
    }

    // 카드 회수/제거 시
    public void ClearSlot()
    {
        isOccupied = false;
        currentCard = null;

        // 카드가 치워졌으므로 슬롯 기본 공격력 텍스트 다시 노출
        if (baseAtkText != null)
        {
            baseAtkText.gameObject.SetActive(true);
        }
    }

    public void SetSlotType(OwnerType newType, int baseAttack)
    {
        slotOwnerType = newType;
        slotBaseAttack = baseAttack;    // 슬롯 기본 공격력 할당
        isOccupied = false;

        // 슬롯 기본 공격력 텍스트 업데이트 및 활성화
        if (baseAtkText != null)
        {
            baseAtkText.gameObject.SetActive(true);
            baseAtkText.text = $"{slotBaseAttack}";

            // (선택) 타입별 텍스트 색상 구분: 플레이어는 하늘색, 적은 주홍/붉은색
            baseAtkText.color = (newType == OwnerType.Player) ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.2f, 0.4f);
        }

        // 슬롯 시각 피드백 (예: 플레이어는 파란색/보라색, 적은 붉은색 계열)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = (newType == OwnerType.Player) ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.2f, 0.4f);
        }
    }
}