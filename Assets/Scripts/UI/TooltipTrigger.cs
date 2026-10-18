using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스를 올렸을 때 툴팁을 띄우고 싶은 모든 UI/오브젝트에 부착
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea(2, 5)]
    [SerializeField] private string tooltipText;

    public void SetTooltipText(string text)
    {
        tooltipText = text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipPanel.Instance != null && !string.IsNullOrEmpty(tooltipText))
        {
            TooltipPanel.Instance.ShowTooltip(tooltipText);
        }
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