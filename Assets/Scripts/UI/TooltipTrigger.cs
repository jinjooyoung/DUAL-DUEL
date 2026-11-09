using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스를 올렸을 때 툴팁 문자열을 동적으로 만들어주는 컴포넌트용 인터페이스 (버프 아이콘 등)
/// 같은 오브젝트에 붙어 있으면 TooltipTrigger가 툴팁을 띄우기 직전에 호출함
/// </summary>
public interface ITooltipTextProvider
{
    string GetTooltipText();
}

/// <summary>
/// 마우스를 올렸을 때 툴팁을 띄우고 싶은 모든 UI/오브젝트에 부착
/// 문자열 우선순위: isCard(카드 데이터로 생성) > ITooltipTextProvider(동적 생성) > 인스펙터 고정 문자열
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea(2, 5)]
    [SerializeField] private string tooltipText;

    [Header("카드 툴팁")]
    [Tooltip("체크 시 같은 오브젝트의 CardDisplay 데이터로 설명을 만들어 표시 (카드 프리팹에서만 체크)")]
    [SerializeField] private bool isCard;
    [SerializeField] private string cardTableName = CardTooltipText.DEFAULT_TABLE;

    private CardDisplay cardDisplay;
    private ITooltipTextProvider textProvider;

    private void Awake()
    {
        if (isCard) cardDisplay = GetComponent<CardDisplay>();
        textProvider = GetComponent<ITooltipTextProvider>();
    }

    public void SetTooltipText(string text)
    {
        tooltipText = text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipPanel.Instance == null) return;

        if (isCard)
        {
            if (!TryBuildCardTooltip()) return;
        }
        else if (textProvider != null)
        {
            SetTooltipText(textProvider.GetTooltipText());
        }

        if (!string.IsNullOrEmpty(tooltipText))
        {
            TooltipPanel.Instance.ShowTooltip(tooltipText);
        }
    }

    /// <summary>
    /// 현재 카드 데이터(강화 단계 반영)로 "타입 설명 \n 기믹 설명" 문자열 생성
    /// </summary>
    private bool TryBuildCardTooltip()
    {
        if (cardDisplay == null) cardDisplay = GetComponent<CardDisplay>();
        if (cardDisplay == null || cardDisplay.cardInstance == null) return false;

        // 드래그 중에는 툴팁이 카드를 가리지 않도록 표시하지 않음
        if (cardDisplay.isDragging) return false;

        CardSO data = cardDisplay.cardInstance.baseData;
        if (data == null) return false;

        SetTooltipText(CardTooltipText.Build(cardDisplay.cardInstance, cardTableName));
        return true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipPanel.Instance != null)
        {
            TooltipPanel.Instance.HideTooltip();
        }
    }

    private void OnDisable()
    {
        if (TooltipPanel.Instance != null)
        {
            TooltipPanel.Instance.HideTooltip();
        }
    }
}
