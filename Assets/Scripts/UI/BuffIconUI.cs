using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체력바 하단 BuffIcons(Horizontal Layout Group) 아래에 들어가는 버프/디버프 아이콘 1개
/// 수치가 1 이상이면 켜고, 0이면 꺼서 레이아웃이 자동으로 당겨지도록 함
/// </summary>
[RequireComponent(typeof(Image))]
public class BuffIconUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI valueText;

    /// <param name="value">버프/디버프 수치</param>
    /// <param name="isPercent">true면 "{수치}%"로 표시 (흡혈)</param>
    public void Refresh(int value, bool isPercent)
    {
        bool isActive = value >= 1;
        if (gameObject.activeSelf != isActive) gameObject.SetActive(isActive);
        if (!isActive) return;

        if (valueText != null)
            valueText.text = isPercent ? $"{value}%" : value.ToString();
    }
}
