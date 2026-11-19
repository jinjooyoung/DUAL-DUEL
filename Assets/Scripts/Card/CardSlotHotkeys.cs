using UnityEngine;

/// <summary>
/// 전투 단축키
///   슬롯 배치: 마우스가 카드 위에 있을 때 키를 누르면 그 카드를 해당 슬롯에 바로 넣음
///     1 / S  -> 슬롯 1     2 / D -> 슬롯 2     3 / F -> 슬롯 3     4 / G -> 슬롯 4     5 / H -> 슬롯 5   (숫자패드 1~5 도 지원)
///   턴 종료: Space (드래그 중이면 들고 있던 카드를 제자리로, 호버 중이면 호버 연출을 끝내고 턴 종료)
/// 배치 처리는 드래그로 놓을 때와 같은 CardDisplay.TryPlaceToSlot 을 사용 (배치 금지, 교체, 연출, 손패 갱신 모두 동일).
/// 전투 씬의 아무 오브젝트에 하나만 붙이면 됨 (BattleSlotManager 와 같은 오브젝트 권장).
/// </summary>
public class CardSlotHotkeys : MonoBehaviour
{
    [Header("슬롯별 단축키 (인덱스 = 슬롯 순서, 왼쪽부터)")]
    [SerializeField] private KeyCode[] numberKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };
    [SerializeField] private KeyCode[] homeRowKeys = { KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H };
    [SerializeField] private KeyCode[] keypadKeys = { KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5 };

    [Header("턴 종료 단축키")]
    [SerializeField] private KeyCode turnEndKey = KeyCode.Space;

    private void Update()
    {
        // 턴 종료는 슬롯 단축키와 상관없이 먼저 확인
        if (Input.GetKeyDown(turnEndKey)) TurnEndToHotKey();

        int slotIndex = GetPressedSlotIndex();
        if (slotIndex < 0) return;          // 이번 프레임에 슬롯 단축키가 안 눌림

        TryPlaceHoveredCard(slotIndex);
    }

    /// <summary>
    /// 단축키 턴 종료. 턴 실행 중, 게임 오버, 대사창이 떠 있을 때는 무시.
    /// 드래그 중이면 들고 있던 카드를 제자리로 돌려놓고, 호버 중이면 호버 연출을 끝낸 뒤 턴 종료
    /// (턴이 시작되면 OnMouseUp / OnMouseExit 가 막히므로 그 전에 정리)
    /// </summary>
    public void TurnEndToHotKey()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.isGameOver || battle.isTurnExecuting) return;
        if (DialogueManager.IsBlockingInput) return;   // 대사창을 넘기려고 누른 스페이스바로 턴이 끝나지 않게

        // 1. 들고 있던 카드: 슬롯에서 집었으면 그 슬롯으로, 손패에서 집었으면 손패로 (콜라이더도 복구)
        CancelAllDrags();

        // 2. 호버 중인 카드: 확대/들림을 원래대로 (턴 실행 중에는 OnMouseDown 이 막혀 집을 수 없음)
        if (CardDisplay.Hovered != null) CardDisplay.Hovered.CancelHover();

        battle.TurnEnd();
    }

    /// <summary>
    /// 호버 중인 카드를 slotIndex(0부터) 슬롯에 넣기. 넣었으면 true (UI 버튼 등 다른 입력에서도 호출 가능)
    /// </summary>
    public bool TryPlaceHoveredCard(int slotIndex)
    {
        CardDisplay card = CardDisplay.Hovered;
        if (!CanPlace(card)) return false;

        BattleSlotManager slotManager = BattleSlotManager.Instance;
        if (slotManager == null || slotIndex < 0 || slotIndex >= slotManager.slots.Count) return false;

        BattleSlot slot = slotManager.slots[slotIndex];
        if (slot == null) return false;

        bool placed = card.TryPlaceToSlot(slot);
        if (placed) SoundManager.Instance.PlaySFX(SFXType.CardPlay);
        return placed;
    }

    // 드래그 중인 카드를 모두 제자리로
    private static void CancelAllDrags()
    {
        BattleCardManager cards = BattleCardManager.Instance;
        if (cards == null) return;
        foreach (CardDisplay card in cards.cardPool)
        {
            if (card != null && card.gameObject.activeInHierarchy && card.isDragging) card.CancelDrag();
        }
    }

    // 이번 프레임에 눌린 슬롯 단축키의 인덱스 (없으면 -1)
    private int GetPressedSlotIndex()
    {
        int index = FindPressed(numberKeys);
        if (index < 0) index = FindPressed(homeRowKeys);
        if (index < 0) index = FindPressed(keypadKeys);
        return index;
    }

    // 키 배열에서 이번 프레임에 눌린 키의 인덱스
    private static int FindPressed(KeyCode[] keys)
    {
        if (keys == null) return -1;
        for (int i = 0; i < keys.Length; i++)
            if (Input.GetKeyDown(keys[i])) return i;
        return -1;
    }

    // 드래그로 놓을 수 있는 상황과 같은 조건 (CardDisplay 의 OnMouseDown / OnMouseUp 과 동일) + 대사창
    private static bool CanPlace(CardDisplay card)
    {
        if (card == null || !card.gameObject.activeInHierarchy) return false;
        if (card.isDragging || card.isTweening) return false;
        if (BattleManager.Instance == null || BattleManager.Instance.isGameOver || BattleManager.Instance.isTurnExecuting) return false;
        if (DialogueManager.IsBlockingInput) return false;
        return true;
    }
}
