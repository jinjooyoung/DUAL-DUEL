using DG.Tweening;
using UnityEngine;

public class CardDisplay : MonoBehaviour
{
    [Header("카드 데이터(SO)")]
    public CardInstance cardInstance;
    public int cardIndex;
    public BattleSlot currentSlot;

    [Header("표시 담당 컴포넌트 (비우면 같은 오브젝트에서 자동 탐색)")]
    [SerializeField] private CardVisual visual;

    [Header("상태 플래그")]
    public bool isDragging = false;
    public bool isPlaced = false;      // 슬롯에 고정된 상태인지 여부
    public bool isSnapFixed = false;    // 핸드 카드 순서 변경. true일 시 Lerp X
    public bool isTweening = false; // 드로우/디스카드 등 트윈 제어 중일 때 true

    private Vector3 originalPosition;
    private Vector3 originalScale = Vector3.one;

    [Header("레이어 마스크")]
    public LayerMask slotLayer;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (visual == null) visual = GetComponent<CardVisual>();
    }

    void Start()
    {
        slotLayer = LayerMask.GetMask("Slot");
    }

    // 카드 데이터 설정 (외형 갱신은 CardVisual에 위임)
    public void SetupCard(CardInstance instance)
    {
        cardInstance = instance;
        if (cardInstance == null || cardInstance.baseData == null) return;

        if (visual == null) visual = GetComponent<CardVisual>();
        if (visual != null) visual.Render(cardInstance);
    }

    /// <summary>
    /// 강화 단계가 반영된 밸류 / 리워드 파람 텍스트 갱신 (강화 로직에서 호출)
    /// </summary>
    public void RefreshValueTexts()
    {
        if (cardInstance == null || cardInstance.baseData == null) return;
        if (visual != null) visual.RefreshValues(cardInstance);
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
        if (BattleManager.Instance.isGameOver) return;
        if (BattleManager.Instance.isTurnExecuting) return;
        if (isDragging || isPlaced || isTweening) return;

        // 호버 시작 시 현재 손패 위치를 기준점으로 저장
        originalPosition = transform.position;

        DOTweenManager.CardHover(transform, originalPosition, originalScale, 1.08f, 0.2f, 0.12f, 0.04f, 0.8f);
    }

    private void OnMouseExit()
    {
        if (BattleManager.Instance.isGameOver) return;
        if (BattleManager.Instance.isTurnExecuting) return;
        if (isDragging || isPlaced || isTweening) return;
        // 저장해 둔 손패 원래 위치(hoverOriginPos)와 원래 스케일, 회전값으로 깔끔하게 복귀
        DOTweenManager.CardHoverExit(transform, new Vector3(transform.position.x, originalPosition.y, transform.position.z), originalScale, Vector3.zero, 0.12f);
    }

    private void OnMouseDown()
    {
        if (BattleManager.Instance.isGameOver) return;
        if (BattleManager.Instance.isTurnExecuting) return;
        if (isTweening) return;
        DOTween.Kill(GetInstanceID() + "_card"); // 기존 호버 트윈 정리

        isDragging = true;
        if (TooltipPanel.Instance != null) TooltipPanel.Instance.HideTooltip();   // 드래그 시작 시 카드 툴팁 닫기
        SoundManager.Instance.PlaySFX(SFXType.CardPlay);

        foreach (var card in BattleCardManager.Instance.cardPool)
        {
            ColliderOnOff(card, false);
        }

        DOTweenManager.CardDragStart(transform, originalScale * 1.05f, 0.08f);
    }

    private void OnMouseDrag()
    {
        if (BattleManager.Instance.isGameOver) return;
        if (BattleManager.Instance.isTurnExecuting) return;

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
        if (BattleManager.Instance.isGameOver) return;
        if (BattleManager.Instance.isTurnExecuting) return;

        if (!isDragging) return;
        isDragging = false;

        foreach (var card in BattleCardManager.Instance.cardPool)
        {
            if (card == this) continue;

            ColliderOnOff(card, true);
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 마우스 뗀 자리에 슬롯이 있는 경우
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, slotLayer))
        {
            BattleSlot targetSlot = hit.collider.GetComponent<BattleSlot>();

            // [RestrictCardType] 배치 금지 타입이면 슬롯 배치 자체를 하지 않고 아래 핸드 복귀 처리로 넘어감
            if (targetSlot != null && IsPlacementRestricted())
            {
                targetSlot = null;
            }

            if (targetSlot != null)
            {
                // [분기 A] 원래 꽂혀있던 동일 슬롯에 그대로 다시 내려놓은 경우
                if (targetSlot == currentSlot)
                {
                    DOTweenManager.CardPlace(transform, targetSlot.transform.position, targetSlot.transform.eulerAngles, originalScale, 0.15f, () => ColliderOnOff(this, true));
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

                // 슬롯에 카드가 성공적으로 놓였을 때 호출
                if (BattleCardManager.Instance != null && this.cardInstance != null)
                {
                    // 손패 데이터 리스트에서 즉시 제거 (실제 오브젝트는 슬롯에 존재)
                    BattleCardManager.Instance.handCards.Remove(this.cardInstance);
                    BattleUIManager.Instance?.UpdateDeckUI();
                }

                // 슬롯 안착 자석 연출 + 슬롯 펀치 반응
                DOTweenManager.CardPlace(transform, targetSlot.transform.position, targetSlot.transform.eulerAngles, originalScale, 0.15f, () => ColliderOnOff(this, true));
                DOTweenManager.SlotCardPlaced(targetSlot.transform);

                return;
            }
        }

        SoundManager.Instance.PlaySFX(SFXType.CardPlace);

        // 마우스 뗀 자리가 슬롯이 아닌 경우 (허공 또는 핸드 영역)
        DetachFromCurrentSlot();

        // 카드를 놓은 Y 위치가 -1.5 이하인 경우 핸드 덱 리스트 인덱스 순서 변경
        if (transform.position.y <= -1.5f)
        {
            isSnapFixed = true;
            BattleCardManager.Instance?.ReorderHandIndexOnly(this);
        }

        DOTweenManager.CardDragEnd(transform, originalScale, 0.1f, () => ColliderOnOff(this, true));
    }

    /// <summary>
    /// [RestrictCardType] 몬스터 기믹으로 이 카드 타입의 배치가 금지되었는지 (금지 시 피드백 표시)
    /// </summary>
    private bool IsPlacementRestricted()
    {
        if (cardInstance == null || cardInstance.baseData == null) return false;
        if (!BattleManager.Instance.IsCardTypeRestricted(cardInstance.baseData.cardType)) return false;

        Debug.Log($"[몬스터 기믹: RestrictCardType] {cardInstance.baseData.cardType} 타입은 이번 전투에서 배치할 수 없습니다.");
        BattleUIManager.Instance?.ShowStatusText(transform.position, "배치 불가", new Color(1f, 0.3f, 0.3f));
        return true;
    }

    private void ColliderOnOff(CardDisplay obj, bool on)
    {
        var collider = obj.GetComponent<BoxCollider2D>();
        collider.enabled = on;
    }
}
