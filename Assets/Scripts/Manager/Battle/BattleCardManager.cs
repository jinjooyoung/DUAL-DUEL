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

    [Header("고정 카드 풀 (씬에 배치된 Display 컴포넌트 목록)")]
    [Tooltip("최대 7개 할당 (기본 6개 사용, 확장 여유분 1개)")]
    public List<CardDisplay> cardPool = new List<CardDisplay>();

    [Header("손패 배치 기준점 및 덱/버림 위치")]
    public Transform handPosition;
    [Tooltip("드로우 덱 오브젝트 위치 (없으면 핸드 좌하단 기본값)")]
    public Transform drawDeckTransform;
    [Tooltip("버림 덱 오브젝트 위치 (없으면 핸드 우하단 기본값)")]
    public Transform discardDeckTransform;

    [Header("핸드 정렬 옵션")]
    [SerializeField] private float cardSpacing = 2.0f;
    [SerializeField] private float arrangeSpeed = 10.0f;

    [Header("드로우 / 디스카드 연출 딜레이")]
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
        StartCoroutine(Co_DrawCards(6));
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
            Vector3 targetPos = handPosition.position + new Vector3(startX + ((totalCards - 1) * cardSpacing), 0, 0);

            // 드로우 연출 시작 (DOTweenManager 호출)
            drawnDisplay.isTweening = true;
            Tween drawTween = DOTweenManager.CardDraw(
                drawnDisplay.transform,
                startSpawnPos,
                targetPos,
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
            });

            if (discardTween != null)
            {
                yield return discardTween.WaitForCompletion();
            }

            BattleUIManager.Instance?.UpdateDeckUI();
            yield return new WaitForSeconds(discardInterval);
        }

        Debug.Log("[손패 정리 완료] 모든 잔여 손패 정리 완료");
    }

    private CardDisplay DrawCardInternal()
    {
        CardDisplay availableDisplay = cardPool.Find(c => !c.gameObject.activeSelf);
        if (availableDisplay == null || handCards.Count >= 6) return null;

        if (drawDeck.Count == 0)
        {
            RecycleDiscardToDraw();
            if (drawDeck.Count == 0) return null;
        }

        CardSO drawnData = drawDeck[0];
        drawDeck.RemoveAt(0);
        handCards.Add(drawnData);

        availableDisplay.gameObject.SetActive(true);
        availableDisplay.SetupCard(drawnData);
        availableDisplay.cardIndex = handCards.Count - 1;

        return availableDisplay;
    }

    private void ArrangeHand()
    {
        // 배치되지 않았고, 드래그 중이 아니며, 드로우/디스카드 트윈 연출 중이 아닌 카드만 보간 정렬
        List<CardDisplay> activeHandDisplays = cardPool.FindAll(c => c != null && c.gameObject.activeSelf && !c.isPlaced && !c.isDragging && !c.isTweening);
        if (activeHandDisplays.Count == 0 || handPosition == null) return;

        float totalWidth = (activeHandDisplays.Count - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < activeHandDisplays.Count; i++)
        {
            CardDisplay display = activeHandDisplays[i];
            Vector3 targetPosition = handPosition.position + new Vector3(startX + (i * cardSpacing), 0, 0);
            display.transform.position = Vector3.Lerp(display.transform.position, targetPosition, Time.deltaTime * arrangeSpeed);
        }
    }

    public void DiscardCard(CardDisplay targetDisplay)
    {
        if (targetDisplay == null || !targetDisplay.gameObject.activeSelf) return;

        Vector3 discardTargetPos = discardDeckTransform != null
            ? discardDeckTransform.position
            : (handPosition != null ? handPosition.position + new Vector3(8f, -3f, 0f) : Vector3.zero);

        if (targetDisplay.currentSlot != null)
        {
            targetDisplay.currentSlot.ClearSlot();
        }

        if (targetDisplay.cardSO != null)
        {
            handCards.Remove(targetDisplay.cardSO);
            discardDeck.Add(targetDisplay.cardSO);
        }

        Tween discardTween = DOTweenManager.CardDiscard(targetDisplay.gameObject, discardTargetPos, 0.25f, () =>
        {
            targetDisplay.ResetPlacement();
            targetDisplay.isTweening = false;
        });
        targetDisplay.ResetPlacement();
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