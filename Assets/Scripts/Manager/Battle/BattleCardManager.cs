using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 중 단일 드로우 덱, 핸드, 버림 덱을 관리하고 카드 오브젝트 풀을 제어하는 매니저 클래스입니다.
/// </summary>
public class BattleCardManager : MonoBehaviour
{
    public static BattleCardManager Instance { get; private set; }

    [Header("덱 데이터")]
    [Tooltip("뽑을 카드 더미")]
    public List<CardSO> drawDeck = new List<CardSO>();
    [Tooltip("현재 핸드에 들고 있는 카드 데이터")]
    public List<CardSO> handCards = new List<CardSO>();
    [Tooltip("사용되거나 버려진 카드 더미")]
    public List<CardSO> discardDeck = new List<CardSO>();

    [Header("카드 풀 및 프리팹")]
    [Tooltip("풀이 고갈되었을 때 새로 동적 인스턴스화할 카드 프리팹")]
    [SerializeField] private GameObject cardPrefab;
    [Tooltip("카드 오브젝트 풀 (초기 6개 배치 + 필요 시 동적 확장)")]
    public List<CardDisplay> cardPool = new List<CardDisplay>();

    [Header("덱/버림 위치")]
    [Tooltip("드로우 덱 오브젝트 위치 (없으면 핸드 좌하단 기본값)")]
    public Transform drawDeckTransform;
    [Tooltip("버림 덱 오브젝트 위치 (없으면 핸드 우하단 기본값)")]
    public Transform discardDeckTransform;

    [Header("손패 정렬 기준점")]
    public Transform handPosition;
    [Tooltip("손패가 위치할 수 있는 좌측 최대 경계")]
    [SerializeField] private Transform leftBoundary;
    [Tooltip("손패가 위치할 수 있는 우측 최대 경계")]
    [SerializeField] private Transform rightBoundary;

    [Tooltip("카드 간의 최대(기본) 간격")]
    [SerializeField] private float cardSpacing = 2.0f;
    [SerializeField] private float arrangeSpeed = 10.0f;

    [Header("드로우 / 디스카드 연출 딜레이")]
    public int drawCount = 6;                               // 드로우 할 카드 수
    [SerializeField] private float drawInterval = 0.5f;     // 드로우 간격 (0.5초)
    [SerializeField] private float discardInterval = 0.5f;  // 버리기 간격 (0.5초)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        foreach (var card in cardPool)
        {
            if (card != null) card.gameObject.SetActive(false);
        }

        ShuffleDeck();
        StartCoroutine(Co_DrawCards(drawCount));
    }

    private void Update()
    {
        ArrangeHand();
    }

    /// <summary>
    /// 지정된 장수만큼 DOTweenManager.CardDraw를 통해 순차 드로우
    /// </summary>
    public IEnumerator Co_DrawCards(int count)
    {
        Vector3 startSpawnPos = drawDeckTransform != null
            ? drawDeckTransform.position
            : (handPosition != null ? handPosition.position + new Vector3(-8f, -3f, 0f) : Vector3.zero);

        for (int i = 0; i < count; i++)
        {
            CardDisplay drawnDisplay = DrawCardInternal();
            if (drawnDisplay == null) yield break;

            // 손패 최종 목표 좌표 미리 계산
            int totalCards = handCards.Count;
            float totalWidth = (totalCards - 1) * cardSpacing;
            float startX = -totalWidth / 2f;

            // 드로우 연출 시작 (DOTweenManager 호출)
            drawnDisplay.isTweening = true;
            Tween drawTween = DOTweenManager.CardDraw(
                drawnDisplay.transform,
                startSpawnPos,
                rightBoundary.position,
                Vector3.one,
                0.3f
            );

            BattleUIManager.Instance?.UpdateDeckUI();

            // 트윈 완료 시 정렬 참여 허용
            if (drawTween != null)
            {
                drawTween.OnComplete(() => drawnDisplay.isTweening = false);
            }
            else
            {
                drawnDisplay.isTweening = false;
            }

            yield return new WaitForSeconds(drawInterval);
        }
    }

    /// <summary>
    /// 남아있는 모든 손패 카드를 버림 덱으로 순차 축소/페이드아웃 이동
    /// </summary>
    public IEnumerator Co_DiscardAllHandCards()
    {
        Vector3 discardTargetPos = discardDeckTransform != null
            ? discardDeckTransform.position
            : (handPosition != null ? handPosition.position + new Vector3(8f, -3f, 0f) : Vector3.zero);

        while (true)
        {
            List<CardDisplay> activeHandDisplays = cardPool.FindAll(c => c != null && c.gameObject.activeSelf && !c.isPlaced);
            if (activeHandDisplays.Count == 0) break;

            CardDisplay targetCard = activeHandDisplays[0];
            targetCard.isTweening = true;

            // 데이터 리스트 이동
            if (targetCard.cardSO != null)
            {
                handCards.Remove(targetCard.cardSO);
                discardDeck.Add(targetCard.cardSO);
            }

            // DOTweenManager.CardDiscard 실행 후 완료 대기
            Tween discardTween = DOTweenManager.CardDiscard(targetCard.gameObject, discardTargetPos, 0.25f, () =>
            {
                targetCard.ResetPlacement();
                targetCard.isTweening = false;
                targetCard.gameObject.SetActive(false); // 풀 반환을 위해 명시적으로 비활성화
            });

            if (discardTween != null)
            {
                yield return discardTween.WaitForCompletion();
            }

            BattleUIManager.Instance?.UpdateDeckUI();
            yield return new WaitForSeconds(discardInterval);
        }

        Debug.Log($"[손패 정리 완료] 모든 잔여 손패 버림 덱으로 이동 및 풀 반환 완료 (현재 풀 총량: {cardPool.Count})");
    }

    private CardDisplay DrawCardInternal()
    {
        // 덱 리사이클 확인
        if (drawDeck.Count == 0)
        {
            RecycleDiscardToDraw();
            if (drawDeck.Count == 0) return null;
        }

        // 동적 풀에서 카드 확보 (부족 시 생성)
        CardDisplay availableDisplay = GetOrCreateCardDisplay();
        if (availableDisplay == null) return null;

        CardSO drawnData = drawDeck[0];
        drawDeck.RemoveAt(0);
        handCards.Add(drawnData);

        availableDisplay.gameObject.SetActive(true);
        availableDisplay.SetupCard(drawnData);
        availableDisplay.cardIndex = handCards.Count - 1;

        return availableDisplay;
    }

    /// <summary>
    /// 풀에서 꺼져있는 카드를 가져오거나, 없으면 프리팹을 동적으로 새로 생성하여 풀에 추가 후 반환합니다.
    /// </summary>
    private CardDisplay GetOrCreateCardDisplay()
    {
        // 1. 기존 풀에서 비활성화된 오브젝트 탐색
        CardDisplay availableDisplay = cardPool.Find(c => c != null && !c.gameObject.activeSelf);

        if (availableDisplay != null)
        {
            return availableDisplay;
        }

        // 2. 여유분이 없으면 동적 확장 생성
        if (cardPrefab != null)
        {
            Transform parentTransform = handPosition != null ? handPosition.parent : transform;
            GameObject newCardObj = Instantiate(cardPrefab, parentTransform);
            newCardObj.SetActive(false);

            CardDisplay newDisplay = newCardObj.GetComponent<CardDisplay>();
            if (newDisplay != null)
            {
                cardPool.Add(newDisplay);
                Debug.Log($"[카드 풀 동적 확장] 여유 카드 부족으로 새 카드 생성. 현재 풀 크기: {cardPool.Count}");
                return newDisplay;
            }
        }

        Debug.LogError("[CardPool] cardPrefab이 연결되지 않아 카드를 동적으로 생성할 수 없습니다.");
        return null;
    }

    private void ArrangeHand()
    {
        // 배치되지 않았고, 드래그 중이 아니며, 드로우/디스카드 트윈 연출 중이 아닌 카드만 보간 정렬
        List<CardDisplay> activeHandDisplays = cardPool.FindAll(c =>
            c != null &&
            c.gameObject.activeSelf &&
            !c.isPlaced &&
            c.currentSlot == null &&
            !c.isDragging &&
            !c.isTweening
        );

        if (activeHandDisplays.Count == 0 || handPosition == null) return;

        int cardCount = activeHandDisplays.Count;

        // 카드가 1장이면 핸드 기준점 중앙에 바로 배치
        if (cardCount == 1)
        {
            Vector3 centerPos = handPosition.position;
            activeHandDisplays[0].transform.position = Vector3.Lerp(activeHandDisplays[0].transform.position, centerPos, Time.deltaTime * arrangeSpeed);
            return;
        }

        // 1. 최대 허용 너비 계산 (경계 트랜스폼이 할당되어 있다면 그 거리, 없으면 기본값)
        float maxAvailableWidth = (leftBoundary != null && rightBoundary != null)
            ? Mathf.Abs(rightBoundary.position.x - leftBoundary.position.x)
            : (cardCount - 1) * cardSpacing;

        // 2. 동적 간격(Spacing) 계산: 기본 간격으로 배치했을 때 경계를 넘치면 비율에 맞게 줄임
        float actualSpacing = cardSpacing;
        float defaultTotalWidth = (cardCount - 1) * cardSpacing;

        if (defaultTotalWidth > maxAvailableWidth)
        {
            // 경계 폭을 넘어가면 카드 수에 맞춰 간격을 좁힘
            actualSpacing = maxAvailableWidth / (cardCount - 1);
        }

        // 3. 중앙 기준 시작 X좌표 계산
        float finalTotalWidth = (cardCount - 1) * actualSpacing;
        float startX = -finalTotalWidth / 2f;

        // 경계의 중심 X좌표 기준 (경계가 없으면 handPosition 기준)
        float centerX = (leftBoundary != null && rightBoundary != null)
            ? (leftBoundary.position.x + rightBoundary.position.x) / 2f
            : handPosition.position.x;

        for (int i = 0; i < cardCount; i++)
        {
            CardDisplay display = activeHandDisplays[i];

            // 카드가 겹칠 때 우측 카드가 좌측 카드 위에 자연스럽게 얹히도록 Z 오프셋 살짝 부여 (-0.01f씩 앞당김)
            Vector3 targetPosition = new Vector3(
                centerX + startX + (i * actualSpacing),
                handPosition.position.y,
                handPosition.position.z - (i * 0.01f)
            );

            display.transform.position = Vector3.Lerp(display.transform.position, targetPosition, Time.deltaTime * arrangeSpeed);
        }
    }

    /// <summary>
    /// 놓은 카드의 X 좌표를 기준으로 handCards와 cardPool의 인덱스 순서를 변경합니다.
    /// </summary>
    public void ReorderHandIndexOnly(CardDisplay releasedCard)
    {
        if (releasedCard == null || releasedCard.cardSO == null) return;

        // 1. 현재 핸드에 있는 카드들 수집 (슬롯에 꽂힌 카드 제외)
        List<CardDisplay> activeCards = cardPool.FindAll(c =>
            c != null &&
            c.gameObject.activeSelf &&
            !c.isPlaced &&
            c.currentSlot == null
        );

        if (activeCards.Count <= 1) return;

        // 2. 자신을 제외한 카드들을 현재 X 좌표(왼쪽 -> 오른쪽) 순으로 정렬
        List<CardDisplay> otherCards = activeCards.FindAll(c => c != releasedCard);
        otherCards.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        // 3. 놓은 카드의 X 좌표와 비교해 들어갈 인덱스 찾기
        float releasedX = releasedCard.transform.position.x;
        int targetIndex = otherCards.Count; // 기본값은 맨 뒤

        for (int i = 0; i < otherCards.Count; i++)
        {
            if (releasedX < otherCards[i].transform.position.x)
            {
                targetIndex = i;
                break;
            }
        }

        // 4. handCards (데이터 리스트) 순서 변경
        if (handCards.Contains(releasedCard.cardSO))
        {
            handCards.Remove(releasedCard.cardSO);
            targetIndex = Mathf.Clamp(targetIndex, 0, handCards.Count);
            handCards.Insert(targetIndex, releasedCard.cardSO);
        }

        // 5. cardPool (오브젝트 풀 리스트) 순서 동기화
        if (cardPool.Contains(releasedCard))
        {
            cardPool.Remove(releasedCard);
            cardPool.Insert(targetIndex, releasedCard);
        }
    }

    public void DiscardCard(CardDisplay targetDisplay)
    {
        if (targetDisplay == null || !targetDisplay.gameObject.activeSelf) return;

        Vector3 discardTargetPos = discardDeckTransform != null
            ? discardDeckTransform.position
            : (handPosition != null ? handPosition.position + new Vector3(8f, -3f, 0f) : Vector3.zero);

        // 연출 플래그 설정 (정렬 대상에서 즉시 제외)
        targetDisplay.isTweening = true;

        if (targetDisplay.currentSlot != null)
        {
            targetDisplay.currentSlot.ClearSlot();
        }

        if (targetDisplay.cardSO != null)
        {
            handCards.Remove(targetDisplay.cardSO);
            discardDeck.Add(targetDisplay.cardSO);
        }

        // [수정] ResetPlacement()를 트윈 시작 전에 호출하지 않고, 
        // 트윈이 끝난 후 비활성화 직전에 호출합니다.
        Tween discardTween = DOTweenManager.CardDiscard(targetDisplay.gameObject, discardTargetPos, 0.25f, () =>
        {
            targetDisplay.ResetPlacement();
            targetDisplay.isTweening = false;
            targetDisplay.gameObject.SetActive(false); // 풀 반환 비활성화
        });

        BattleUIManager.Instance?.UpdateDeckUI();
    }

    private void ShuffleDeck()
    {
        List<CardSO> tempDeck = new List<CardSO>(drawDeck);
        drawDeck.Clear();
        while (tempDeck.Count > 0)
        {
            int randIndex = Random.Range(0, tempDeck.Count);
            drawDeck.Add(tempDeck[randIndex]);
            tempDeck.RemoveAt(randIndex);
        }
    }

    private void RecycleDiscardToDraw()
    {
        if (discardDeck.Count == 0) return;
        drawDeck.AddRange(discardDeck);
        discardDeck.Clear();
        ShuffleDeck();
    }
}