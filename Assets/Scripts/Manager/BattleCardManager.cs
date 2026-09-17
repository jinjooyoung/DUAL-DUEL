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

    [Header("손패 배치 기준점")]
    public Transform handPosition;

    [Header("핸드 정렬 옵션")]
    [SerializeField] private float cardSpacing = 2.0f;
    [SerializeField] private float arrangeSpeed = 10.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ShuffleDeck();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DrawCard();
        }

        ArrangeHand();
    }

    /// <summary>
    /// 현재 드로우 덱에 있는 카드들의 순서를 무작위로 섞습니다.
    /// </summary>
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

        Debug.Log($"덱 셔플 완료: 남은 카드 {drawDeck.Count}장");
    }

    /// <summary>
    /// 버림 덱의 모든 카드를 드로우 덱으로 복구한 뒤 셔플합니다.
    /// </summary>
    private void RecycleDiscardToDraw()
    {
        if (discardDeck.Count == 0)
        {
            Debug.Log("버림 카드 더미가 비어 있어 재활용할 수 없습니다.");
            return;
        }

        drawDeck.AddRange(discardDeck);
        discardDeck.Clear();
        ShuffleDeck();

        Debug.Log($"버림 덱을 드로우 덱으로 회수 및 셔플 완료: 총 {drawDeck.Count}장");
    }

    /// <summary>
    /// 덱에서 카드 1장을 뽑아 비활성화된 풀 오브젝트에 할당하고 손패에 추가합니다.
    /// 덱이 비어 있다면 자동으로 버림 덱을 회수하여 드로우를 시도합니다.
    /// </summary>
    public void DrawCard()
    {
        // 1. 카드 풀에서 비활성화된 Display 검색
        CardDisplay availableDisplay = cardPool.Find(c => !c.gameObject.activeSelf);
        if (availableDisplay == null || handCards.Count >= 6)
        {
            Debug.LogWarning("손패 오브젝트 풀이 가득 차 더 이상 카드를 표시할 수 없습니다.");
            return;
        }

        // 2. 덱 고갈 시 버림 덱 회수 시도
        if (drawDeck.Count == 0)
        {
            RecycleDiscardToDraw();

            if (drawDeck.Count == 0)
            {
                Debug.LogWarning("드로우 덱과 버림 덱이 모두 비어 카드를 뽑을 수 없습니다.");
                return;
            }
        }

        // 3. 데이터 추출 및 핸드 등록
        CardSO drawnData = drawDeck[0];
        drawDeck.RemoveAt(0);
        handCards.Add(drawnData);

        // 4. 오브젝트 활성화 및 데이터 바인딩
        availableDisplay.gameObject.SetActive(true);
        availableDisplay.SetupCard(drawnData);
        availableDisplay.cardIndex = handCards.Count - 1;

        Debug.Log($"카드 드로우 완료: {drawnData.nameKey} (현재 손패: {handCards.Count}장)");
    }

    /// <summary>
    /// 화면에 활성화된 손패 오브젝트들을 중앙 기준으로 정렬합니다.
    /// </summary>
    private void ArrangeHand()
    {
        // 활성화된 카드 중 아직 슬롯에 배치되지 않은(isPlaced == false)카드만 필터링
        List<CardDisplay> activeHandDisplays = GetActiveDisplays().FindAll(c => !c.isPlaced);

        if (activeHandDisplays.Count == 0 || handPosition == null) return;

        float totalWidth = (activeHandDisplays.Count - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < activeHandDisplays.Count; i++)
        {
            CardDisplay display = activeHandDisplays[i];

            // 사용자가 드래그 중인 카드는 위치 보간에서 제외
            if (display.isDragging) continue;

            Vector3 targetPosition = handPosition.position + new Vector3(startX + (i * cardSpacing), 0, 0);
            display.transform.position = Vector3.Lerp(display.transform.position, targetPosition, Time.deltaTime * arrangeSpeed);
        }
    }

    /// <summary>
    /// 특정 카드 오브젝트를 버림 더미로 이동시키고 오브젝트를 풀로 반환(비활성화)합니다.
    /// </summary>
    /// <param name="targetDisplay">버릴 대상 CardDisplay 컴포넌트</param>
    public void DiscardCard(CardDisplay targetDisplay)
    {
        if (targetDisplay == null || !targetDisplay.gameObject.activeSelf) return;

        // 1. 슬롯에 꽂혀 있던 카드라면 슬롯 비우기
        if (targetDisplay.currentSlot != null)
        {
            targetDisplay.currentSlot.ClearSlot();
        }

        // 2. 데이터 리스트 이동: handCards -> discardDeck
        if (targetDisplay.cardSO != null)
        {
            handCards.Remove(targetDisplay.cardSO);
            discardDeck.Add(targetDisplay.cardSO);
        }

        // 3. 카드 상태 초기화 후 오브젝트 비활성화 (풀 반환)
        targetDisplay.ResetPlacement();
        targetDisplay.gameObject.SetActive(false);

        Debug.Log($"카드 버림 완료: {targetDisplay.cardSO?.nameKey} (남은 손패: {handCards.Count}장)");
    }

    /// <summary>
    /// 현재 풀에서 활성화되어 화면에 노출 중인 카드 오브젝트 목록을 반환합니다.
    /// </summary>
    private List<CardDisplay> GetActiveDisplays()
    {
        return cardPool.FindAll(c => c != null && c.gameObject.activeSelf);
    }
}