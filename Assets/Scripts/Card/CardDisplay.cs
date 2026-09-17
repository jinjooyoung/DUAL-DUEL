using System;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class CardDisplay : MonoBehaviour
{
    // [정적 이벤트 선언] (배치된 카드, 배치된 슬롯)을 매개변수로 송출
    public static event Action<CardDisplay, BattleSlot> OnCardPlaced;

    [Header("카드 데이터(SO)")]
    public CardSO cardSO;
    public int cardIndex;
    public BattleSlot currentSlot;

    [Header("이미지")]
    public SpriteRenderer background;
    public SpriteRenderer cardResource;
    public SpriteRenderer typeIcon;

    [Header("텍스트")]
    public TextMeshPro nameText;
    public TextMeshPro typeText;
    public TextMeshPro descriptionText;

    public bool isDragging = false;
    public bool isPlaced = false;      // 슬롯에 고정된 상태인지 여부
    private Vector3 originalPosition;

    [Header("레이어 마스크")]
    public LayerMask slotLayer;

    void Start()
    {
        slotLayer = LayerMask.GetMask("Slot");

        //SetupCard(cardData);
    }

    // 카드 데이터 설정
    public void SetupCard(CardSO data)
    {
        cardSO = data;

        // 3D 텍스트 업데이트
        if (nameText != null) nameText.text = LocalizationManager.Instance.GetText(data.nameKey);
        if (typeText != null) typeText.text = LocalizationManager.Instance.GetText($"{data.cardType.ToString().ToUpper()}_KEY");

        string desTemp = LocalizationManager.Instance.GetText(data.descKey);
        desTemp = desTemp.Replace("[Value]", data.values[0].ToString());

        if (descriptionText != null) descriptionText.text = desTemp;

        // 카드 리소스
        if (cardResource != null && cardSO.artwork != null)
        {
            cardResource.sprite = cardSO.artwork;
        }

        if (background != null)
            background.sprite = Resources.Load<Sprite>("Cards/Public/Card_BG");

        if (typeIcon != null)
            typeIcon.sprite = Resources.Load<Sprite>($"Cards/Public/{cardSO.cardType}");

        // 로컬라이징 후 설명 텍스트의 {value}를 cardSO.values로 Replace해주는 코드 여기 작성해야함
    }

    /// <summary>
    /// 턴 종료 등으로 슬롯이나 매니저에 의해 강제로 배치가 초기화될 때 호출
    /// </summary>
    public void ResetPlacement()
    {
        isPlaced = false;      // "슬롯에 고정됨" 상태를 해제
        currentSlot = null;    // 연결되어 있던 슬롯 참조 제거
        isDragging = false;    // 드래그 중 플래그 안전 초기화
    }

    /// <summary>
    /// 현재 꽂혀있던 슬롯과의 연결을 끊고 핸드로 복귀 가능한 상태로 전환
    /// </summary>
    public void DetachFromCurrentSlot()
    {
        if (currentSlot != null)
        {
            currentSlot.ClearSlot();
            currentSlot = null;
        }
        isPlaced = false;
    }

    private void OnMouseDown()
    {
        // 드래그 시작 시 원래 위치 저장
        originalPosition = transform.position;
        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (isDragging)
        {
            // 마우스 위치로 카드 이동
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 마우스 뗀 자리에 슬롯이 있는 경우
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, slotLayer))
        {
            BattleSlot targetSlot = hit.collider.GetComponent<BattleSlot>();

            if (targetSlot != null)
            {
                // [분기 A] 원래 꽂혀있던 동일 슬롯에 그대로 다시 내려놓은 경우
                if (targetSlot == currentSlot)
                {
                    transform.position = targetSlot.transform.position;
                    return;
                }

                // 기존에 다른 슬롯에 꽂혀있던 카드라면 이전 슬롯과의 연결을 먼저 끊음
                DetachFromCurrentSlot();

                // [분기 B] 목표 슬롯이 이미 다른 카드로 차 있는 경우 (바운스/교체)
                if (targetSlot.isOccupied && targetSlot.currentCard != null)
                {
                    CardDisplay existingCard = targetSlot.currentCard;

                    // 기존에 박혀있던 카드를 슬롯에서 분리 -> 핸드 정렬로 복귀시킴
                    existingCard.DetachFromCurrentSlot();
                }

                // [분기 C] 목표 슬롯에 새 카드 안착
                isPlaced = true;
                currentSlot = targetSlot;
                targetSlot.PlaceCard(this);

                OnCardPlaced?.Invoke(this, targetSlot);
                return;
            }
        }

        // 마우스 뗀 자리가 슬롯이 아닌 경우 (허공 또는 핸드 영역)
        // 슬롯에 꽂혀있던 카드라면 슬롯 연결을 끊고 핸드로 복귀 (ArrangeHand가 알아서 정렬)
        DetachFromCurrentSlot();
    }
}
