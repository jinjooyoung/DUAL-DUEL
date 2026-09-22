using DG.Tweening;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class CardDisplay : MonoBehaviour
{
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

    [Header("상태 플래그")]
    public bool isDragging = false;
    public bool isPlaced = false;      // 슬롯에 고정된 상태인지 여부
    [HideInInspector] public bool isTweening = false; // 드로우/디스카드 등 트윈 제어 중일 때 true

    private Vector3 originalPosition;
    private Vector3 originalScale = Vector3.one;

    [Header("레이어 마스크")]
    public LayerMask slotLayer;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    void Start()
    {
        slotLayer = LayerMask.GetMask("Slot");
    }

    // 카드 데이터 설정
    public void SetupCard(CardSO data)
    {
        cardSO = data;

        // 3D 텍스트 업데이트
        if (nameText != null) nameText.text = LocalizationManager.Instance.GetText(data.nameKey);
        if (typeText != null) typeText.text = LocalizationManager.Instance.GetText($"{data.cardType.ToString().ToUpper()}_KEY");

        string desTemp = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(data.descKey) : data.descKey;
        if (data.values != null && data.values.Count > 0)
        {
            desTemp = desTemp.Replace("[Value]", data.values[0].ToString());
        }

        if (descriptionText != null) descriptionText.text = desTemp;

        // 카드 리소스
        if (cardResource != null && cardSO.artwork != null)
            cardResource.sprite = cardSO.artwork;

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
        isTweening = false;    // 연출 중 아님
        transform.localScale = originalScale;   // 크기 초기화
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

    // 마우스 호버 연출 (슬롯에 꽂히지 않고, 드래그 중이 아닐 때만)
    private void OnMouseEnter()
    {
        if (isDragging || isPlaced || isTweening) return;

        // 호버 시작 시 현재 손패 위치를 기준점으로 저장
        originalPosition = transform.position;

        DOTweenManager.CardHover(transform, originalPosition, originalScale, 1.08f, 0.2f, 0.12f, 0.04f, 0.8f);
    }

    private void OnMouseExit()
    {
        if (isDragging || isPlaced || isTweening) return;
        // 저장해 둔 손패 원래 위치(hoverOriginPos)와 원래 스케일, 회전값으로 깔끔하게 복귀
        DOTweenManager.CardHoverExit(transform, originalPosition, originalScale, Vector3.zero, 0.12f);
    }

    private void OnMouseDown()
    {
        if (isTweening) return;
        DOTween.Kill(GetInstanceID() + "_card"); // 기존 호버 트윈 정리

        isDragging = true;

        DOTweenManager.CardDragStart(transform, originalScale * 1.05f, 0.08f);
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
                    DOTweenManager.CardPlace(transform, targetSlot.transform.position, targetSlot.transform.eulerAngles, originalScale, 0.15f);
                    return;
                }

                // 기존에 다른 슬롯에 꽂혀있던 카드라면 이전 슬롯과의 연결을 먼저 끊음
                DetachFromCurrentSlot();

                // [분기 B] 목표 슬롯이 이미 다른 카드로 차 있는 경우 (바운스/교체)
                if (targetSlot.isOccupied && targetSlot.currentCard != null)
                {
                    targetSlot.currentCard.DetachFromCurrentSlot();
                }

                // [분기 C] 목표 슬롯에 새 카드 안착
                isPlaced = true;
                currentSlot = targetSlot;
                targetSlot.PlaceCard(this);

                // 슬롯 안착 자석 연출 + 슬롯 펀치 반응
                DOTweenManager.CardPlace(transform, targetSlot.transform.position, targetSlot.transform.eulerAngles, originalScale, 0.15f);
                DOTweenManager.SlotCardPlaced(targetSlot.transform);

                return;
            }
        }

        // 마우스 뗀 자리가 슬롯이 아닌 경우 (허공 또는 핸드 영역)
        // 슬롯에 꽂혀있던 카드라면 슬롯 연결을 끊고 핸드로 복귀 (ArrangeHand가 알아서 정렬)
        DetachFromCurrentSlot();
        DOTweenManager.CardDragEnd(transform, originalScale, 0.1f);
    }
}
