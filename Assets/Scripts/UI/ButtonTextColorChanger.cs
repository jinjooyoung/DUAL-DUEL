
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonTextColorChanger : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Button targetButton;
    [SerializeField] private TMP_Text targetText;

    [Header("텍스트 색상")]
    [SerializeField] private Color enabledColor = new Color32(0xA9, 0x0D, 0x01, 0xFF);
    [SerializeField] private Color disabledColor = new Color32(0x37, 0x37, 0x37, 0xFF);

    private bool check = false;

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (targetButton == null || targetText == null)
            return;

        check = targetButton.interactable;
        UpdateTextColor();
    }

    private void Update()
    {
        if (targetButton == null || targetText == null)
            return;

        if (targetButton.interactable == check)
            return;

        check = targetButton.interactable;
        UpdateTextColor();
    }

    private void UpdateTextColor()
    {
        targetText.color = check ? enabledColor : disabledColor;
    }
}
