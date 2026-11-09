using DG.Tweening;
using TMPro;
using UnityEngine;

public class BattleSlot : MonoBehaviour
{
    public OwnerType slotOwnerType;
    public int slotIndex;           // 0 ~ 4 (좌측부터 순서대로)
    public bool isOccupied = false; // 카드가 이미 배치되어 있는지 여부
    public CardDisplay currentCard; // 배치된 카드 참조

    [Header("슬롯 기본 공격력")]
    public int slotBaseAttack;      // 카드가 없을 때 발동할 공격력

    [Header("UI 바인딩")]
    [SerializeField] private TextMeshPro baseAtkText; // 인스펙터에서 직접 할당할 TMP

    [Header("몬스터 기믹: 블라인드")]
    [Tooltip("물음표 마스크 (슬롯보다 위, 카드보다 아래 Sorting Order)")]
    [SerializeField] private SpriteRenderer blindMask;
    public bool isBlind { get; private set; }

    [Header("몬스터 기믹: 밸류 왜곡")]
    [Tooltip("슬롯 밸류 보정값 표시 (+2 / -3)")]
    [SerializeField] private TextMeshPro valueModifierText;
    public int valueModifier { get; private set; }   // 이 슬롯에 놓인 카드 밸류에 더해지는 값 (0이면 없음)

    // 단색 초록 화면에서는 색이 아니라 밝기로 구분해야 하므로 반전 구성
    // 플레이어 슬롯: 밝은 바탕 + 어두운 숫자 / 적 슬롯: 어두운 바탕 + 밝은 숫자 / 블라인드: 중간 밝기 + ?
    private static readonly Color PlayerColor = new Color(0.45f, 0.95f, 0.55f);
    private static readonly Color EnemyColor = new Color(0.07f, 0.24f, 0.11f);
    private static readonly Color BlindColor = new Color(0.2f, 0.42f, 0.25f);
    private static readonly Color PlayerTextColor = new Color(0.03f, 0.12f, 0.05f);
    private static readonly Color EnemyTextColor = new Color(0.65f, 1f, 0.7f);
    private static readonly Color ModifierPlusColor = new Color(0.75f, 1f, 0.8f);
    private static readonly Color ModifierMinusColor = new Color(0.35f, 0.6f, 0.4f);

    private SpriteRenderer slotRenderer;

    private void Awake()
    {
        slotRenderer = GetComponent<SpriteRenderer>();
        if (blindMask != null) blindMask.gameObject.SetActive(false);
        if (valueModifierText != null) valueModifierText.gameObject.SetActive(false);
    }

    private static Color GetOwnerColor(OwnerType type) => type == OwnerType.Player ? PlayerColor : EnemyColor;
    private static Color GetOwnerTextColor(OwnerType type) => type == OwnerType.Player ? PlayerTextColor : EnemyTextColor;

    // 카드 배치 처리
    public void PlaceCard(CardDisplay card)
    {
        isOccupied = true;
        currentCard = card;
        card.transform.position = transform.position; // 슬롯 위치로 1회 고정

        // 카드가 슬롯을 덮었으므로 슬롯 기본 공격력 텍스트 숨김
        if (baseAtkText != null)
        {
            baseAtkText.gameObject.SetActive(false);
        }
    }

    // 카드 회수/제거 시
    public void ClearSlot()
    {
        isOccupied = false;
        currentCard = null;

        // 카드가 치워졌으므로 슬롯 기본 공격력 텍스트 다시 노출 (블라인드 중에는 계속 숨김)
        if (baseAtkText != null)
        {
            baseAtkText.gameObject.SetActive(!isBlind);
        }
    }

    /// <summary>
    /// 턴 실행 시 해당 슬롯 액션 격발 시점에 텍스트를 비움
    /// </summary>
    public void ClearAtkText()
    {
        if (baseAtkText != null)
        {
            baseAtkText.text = "";
            // 또는 baseAtkText.gameObject.SetActive(false);
        }
    }

    public void SetSlotType(OwnerType newType, int baseAttack)
    {
        slotOwnerType = newType;
        slotBaseAttack = baseAttack;    // 슬롯 기본 공격력 할당
        isOccupied = false;

        // 이전 턴 기믹 상태 초기화 (이번 턴 기믹은 BattleSlotManager가 배정 직후 다시 부여)
        ResetGimmickState();

        // 슬롯 기본 공격력 텍스트 업데이트 및 활성화
        if (baseAtkText != null)
        {
            baseAtkText.gameObject.SetActive(true);
            baseAtkText.text = $"{slotBaseAttack}";

            // (선택) 타입별 텍스트 색상 구분: 플레이어는 하늘색, 적은 주홍/붉은색
            baseAtkText.color = GetOwnerTextColor(newType);
        }

        // 슬롯 시각 피드백 (예: 플레이어는 파란색/보라색, 적은 붉은색 계열)
        if (slotRenderer == null) slotRenderer = GetComponent<SpriteRenderer>();
        if (slotRenderer != null)
        {
            slotRenderer.DOKill();
            slotRenderer.color = GetOwnerColor(newType);
        }
    }

    #region 몬스터 기믹

    private void ResetGimmickState()
    {
        isBlind = false;
        if (blindMask != null)
        {
            blindMask.DOKill();
            blindMask.gameObject.SetActive(false);
        }

        SetValueModifier(0);
    }

    /// <summary>
    /// [BlindSlot] true: 물음표 마스크로 슬롯 주인/기본 공격력/밸류 보정을 가림
    /// false: 마스크를 페이드아웃하며 원래 타입 색상 공개
    /// </summary>
    public void SetBlind(bool blind, float fadeDuration = 0.35f)
    {
        isBlind = blind;
        if (slotRenderer == null) slotRenderer = GetComponent<SpriteRenderer>();

        if (blind)
        {
            if (blindMask != null)
            {
                blindMask.DOKill();
                blindMask.gameObject.SetActive(true);
                Color c = blindMask.color;
                blindMask.color = new Color(c.r, c.g, c.b, 1f);
            }

            if (slotRenderer != null)
            {
                slotRenderer.DOKill();
                slotRenderer.color = BlindColor;
            }

            if (baseAtkText != null) baseAtkText.gameObject.SetActive(false);
            RefreshValueModifierText();
            return;
        }

        // 공개: 마스크 페이드아웃 + 원래 색상으로 전환
        if (blindMask != null && blindMask.gameObject.activeSelf)
        {
            blindMask.DOKill();
            blindMask.DOFade(0f, fadeDuration).OnComplete(() => blindMask.gameObject.SetActive(false));
        }

        if (slotRenderer != null)
        {
            slotRenderer.DOKill();
            slotRenderer.DOColor(GetOwnerColor(slotOwnerType), fadeDuration);
        }

        if (baseAtkText != null) baseAtkText.gameObject.SetActive(!isOccupied);
        RefreshValueModifierText();
    }

    /// <summary>
    /// [ScaleSlotValue] 이 슬롯에 놓인 카드 밸류에 더할 값 (0이면 해제)
    /// </summary>
    public void SetValueModifier(int modifier)
    {
        valueModifier = modifier;
        RefreshValueModifierText();
    }

    private void RefreshValueModifierText()
    {
        if (valueModifierText == null) return;

        // 블라인드 중에는 보정값도 비공개
        bool show = valueModifier != 0 && !isBlind;
        valueModifierText.gameObject.SetActive(show);
        if (!show) return;

        valueModifierText.text = valueModifier > 0 ? $"+{valueModifier}" : valueModifier.ToString();
        valueModifierText.color = valueModifier > 0 ? ModifierPlusColor : ModifierMinusColor;
    }

    #endregion
}