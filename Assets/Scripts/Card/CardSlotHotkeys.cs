using UnityEngine;

/// <summary>
/// 단축키 배치: 마우스가 카드 위에 있을 때 키를 누르면 그 카드를 해당 슬롯에 바로 넣는다.
///   1 / S  -> 슬롯 1     2 / D -> 슬롯 2     3 / F -> 슬롯 3     4 / G -> 슬롯 4     5 / H -> 슬롯 5
///   (숫자는 직관용, SDFGH 는 F 의 돌기에 손을 얹고 화면만 보며 누르는 용. 숫자패드 1~5 도 지원)
/// 배치 처리는 드래그로 놓을 때와 같은 CardDisplay.TryPlaceToSlot 을 사용 (배치 금지, 교체, 연출, 손패 갱신 모두 동일).
/// 전투 씬의 아무 오브젝트에 하나만 붙이면 됨 (BattleSlotManager 와 같은 오브젝트 권장).
/// </summary>
public class CardSlotHotkeys : MonoBehaviour
{
    [Header("슬롯별 단축키 (인덱스 = 슬롯 순서, 왼쪽부터)")]
    [SerializeField] private KeyCode[] numberKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };
    [SerializeField] private KeyCode[] homeRowKeys = { KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H };
    [SerializeField] private KeyCode[] keypadKeys = { KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5 };
    [SerializeField] private KeyCode turnEndKey = KeyCode.Space;

    private void Update()
    {
        if (Input.GetKeyDown(turnEndKey))
            TurnEndToHotKey();

        int slotIndex = GetPressedSlotIndex();
        if (slotIndex < 0) return;          // 이번 프레임에 단축키가 안 눌림

        TryPlaceHoveredCard(slotIndex);
    }

    public void TurnEndToHotKey()
    {
        if (BattleManager.Instance.isTurnExecuting) return;

        BattleManager.Instance.TurnEnd();
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

    // 이번 프레임에 눌린 단축키의 슬롯 인덱스 (없으면 -1)
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
