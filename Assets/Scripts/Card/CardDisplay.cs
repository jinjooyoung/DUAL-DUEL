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

    [Header("이미지")]
    public SpriteRenderer background;
    public SpriteRenderer cardResource;
    public SpriteRenderer ownerBorder;
    public SpriteRenderer typeIcon;

    [Header("텍스트")]
    public TextMeshPro nameText;
    public TextMeshPro typeText;
    public TextMeshPro descriptionText;

    private bool isDragging = false;
    private bool isPlaced = false;      // 슬롯에 고정된 상태인지 여부
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
        if (nameText != null) nameText.text = data.nameKey;                         // 추후 로컬라이징 추가 시 키 대신 로컬라이징 데이터베이스에서 찾아, 적용하는 방식으로 수정
        if (typeText != null) typeText.text = data.cardType.ToString();
        if (descriptionText != null) descriptionText.text = data.descKey;

        // 카드 리소스
        if (cardResource != null && cardSO.artwork != null)
        {
            cardResource.sprite = cardSO.artwork;
        }

        if (background != null)
            background.sprite = Resources.Load<Sprite>("Cards/Public/Card_BG");

        if (ownerBorder != null)
            ownerBorder.sprite = Resources.Load<Sprite>($"Cards/Public/ownerBorder_{cardSO.ownerType}");

        if (typeIcon != null)
            typeIcon.sprite = Resources.Load<Sprite>($"Cards/Public/{cardSO.cardType}");

        // 로컬라이징 후 설명 텍스트의 {value}를 cardSO.values로 Replace해주는 코드 여기 작성해야함
    }

    private void OnMouseDown()
    {
        Debug.Log("마우스 다운");

        if (isPlaced) return; // 이미 슬롯에 고정된 카드는 조작 불가. 나중에는 배치된거 다른곳으로 옮기거나 취소 가능하게 할건데 일단 1루프는 고정으로 해둠

        // 드래그 시작 시 원래 위치 저장
        originalPosition = transform.position;
        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (isDragging)
        {
            Debug.Log("마우스 드래그중");

            // 마우스 위치로 카드 이동
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }
    }

    private void OnMouseUp()
    {
        Debug.Log("마우스 업");

        isDragging = false;

        // 레이캐스트로 타겟 감지
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 카드 사용 판정 지역 변수
        bool cardUsed = false;

        /*if (Physics.Raycast(ray, out hit, Mathf.Infinity, slotLayer))
        {
            BattleSlot slot = hit.collider.GetComponent<BattleSlot>();

            // 슬롯 컴포넌트가 있고, 비어 있으며, 카드 OwnerType과 슬롯 타입이 일치할 때
            if (slot != null && !slot.isOccupied && slot.slotOwnerType == cardSO.ownerType)
            {
                isPlaced = true;
                slot.PlaceCard(this); // 슬롯에게 직접 배치 요청
                                      //OnCardPlaced?.Invoke(this, slot);   // 카드 배치 성공(완료) 이벤트 방송 -> 이벤트 아직 안 필요한 것 같아서 주석해둠
                cardUsed = true;
                return;
            }
        }*/

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, slotLayer))
        {
            Debug.Log($"[1. 레이 충돌 성공] 부딪힌 오브젝트: {hit.collider.gameObject.name}, 레이어: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            BattleSlot slot = hit.collider.GetComponent<BattleSlot>();

            // 2. BattleSlot 컴포넌트 부착 여부 확인
            if (slot == null)
            {
                Debug.LogWarning("[2. 실패] 충돌한 오브젝트에 BattleSlot 컴포넌트가 없습니다! (자식/부모 오브젝트 확인 필요)");
            }
            else
            {
                Debug.Log($"[2. 슬롯 발견] slotOwnerType: {slot.slotOwnerType}, isOccupied: {slot.isOccupied} / 카드 ownerType: {cardSO.ownerType}");

                // 3. 조건문 세부 검사
                if (slot.isOccupied)
                {
                    Debug.LogWarning("[3. 실패] 슬롯이 이미 차지되어 있습니다 (isOccupied == true)");
                }
                else if (slot.slotOwnerType != cardSO.ownerType)
                {
                    Debug.LogWarning($"[3. 실패] 타입 불일치! 슬롯 타입({slot.slotOwnerType}) != 카드 타입({cardSO.ownerType})");
                }
                else
                {
                    // 모든 조건 통과
                    Debug.Log("<color=green>[성공] 모든 조건 통과! 슬롯에 배치합니다.</color>");
                    isPlaced = true;
                    slot.PlaceCard(this);
                    return;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[1. 실패] 레이캐스트가 slotLayer({slotLayer.value})에 걸리지 않았습니다. 슬롯에 3D Collider가 있는지, Layer 설정이 맞는지 확인하세요.");
        }

        if (!cardUsed)      // 카드를 사용하지 않았다면 원래 위치로 되돌리기
        {
            transform.position = originalPosition;
            return;
        }
    }
}
