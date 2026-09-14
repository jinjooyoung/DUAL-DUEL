using System.Collections.Generic;
using UnityEngine;

// 배틀 시 드로우, 핸드, 버림 덱을 관리하는 매니저 드로우, 셔플 등
public class BattleCardManager : MonoBehaviour
{
    public static BattleCardManager Instance { get; private set; }

    [Header("플레이어 덱")]
    public List<CardSO> playerDrawDeck = new List<CardSO>();
    public List<CardSO> playerDiscardDeck = new List<CardSO>();

    [Header("적 덱")]
    public List<CardSO> enemyDrawDeck = new List<CardSO>();
    public List<CardSO> enemyDiscardDeck = new List<CardSO>();

    [Header("덱 데이터")]
    public List<CardSO> drawDeck = new List<CardSO>();      // 뽑을 카드 더미
    public List<CardSO> handData = new List<CardSO>();      // 현재 핸드에 들고 있는 데이터
    public List<CardSO> discardDeck = new List<CardSO>();   // 사용/버려진 카드 더미

    [Header("고정 카드 풀 (씬에 배치된 6~7개 프리팹)")]
    public List<CardDisplay> cardPool = new List<CardDisplay>();

    [Header("전투당 리롤 설정")]
    public int maxCombatRerolls = 2;    // 전투당 총 리롤 가능 횟수
    public int remainingRerolls;

    private void Awake()
    {   // 전투 씬에서만 쓰이는 매니저기 때문에 파괴 금지 안 함
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ShuffleDeck();
    }

    // 다른 리스트와 관련 없이 드로우 덱에 있는 카드 순서를 '섞기'만 하는 함수
    public void ShuffleDeck()
    {
        List<CardSO> temp = new List<CardSO>(drawDeck);     // 현재 드로우 덱에 있는 카드 임시 저장
        drawDeck.Clear();                                   // 비움

        // 랜덤한 순서로 섞음
        while (temp.Count > 0)
        {
            int randIndex = Random.Range(0, temp.Count);
            drawDeck.Add(temp[randIndex]);
            temp.RemoveAt(randIndex);
        }
        Debug.Log($"덱 셔플 완료: 남은 카드 {drawDeck.Count}장");
    }

    // 덱 고갈 시 버림 더미를 덱으로 되돌히고 셔플
    public void RecycleDiscardToDraw()
    {
        if (discardDeck.Count == 0) return;

        drawDeck.AddRange(discardDeck);
        discardDeck.Clear();
        ShuffleDeck();
    }

    // 카드 드로우
    public void DrawCard()
    {
        // 비어있는 카드 오브젝트(풀) 찾기
        CardDisplay availableDisplay = cardPool.Find(c => !c.gameObject.activeSelf);
        if (availableDisplay == null)
        {
            Debug.Log("손패가 가득 찼습니다.");
            return;
        }

        // 덱이 비었으면 버림 더미 리롤
        if (drawDeck.Count == 0)
        {
            RecycleDiscardToDraw();
            if (drawDeck.Count == 0)
            {
                Debug.Log("더 이상 뽑을 카드가 없습니다.");
                return;
            }
        }

        // 덱 맨 위에서 데이터 추출
        CardSO drawnData = drawDeck[0];
        drawDeck.RemoveAt(0);
        handData.Add(drawnData);

        // 풀에 있던 카드 오브젝트 활성화 및 데이터 덮어쓰기
        availableDisplay.gameObject.SetActive(true);
        availableDisplay.SetupCard(drawnData);
        //availableDisplay.ResetToHand(); // 핸드 기본 위치로 초기화
    }
}
