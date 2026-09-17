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
        if (!isDragging) return;
        isDragging = false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, slotLayer))
        {
            BattleSlot slot = hit.collider.GetComponent<BattleSlot>();

            // [변경] 카드의 ownerType 검사 제거!
            // 슬롯이 존재하고 비어있기만 하면 어디든(적 슬롯이든 아군 슬롯이든) 배치 가능
            if (slot != null && !slot.isOccupied)
            {
                isPlaced = true;
                currentSlot = slot;
                slot.PlaceCard(this); // 슬롯에 안착
                return;
            }
        }

        // 빈 슬롯에 닿지 않았으면 원위치 복귀
        transform.position = originalPosition;
    }
}
