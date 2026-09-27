using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIViewerCard : MonoBehaviour
{
    [SerializeField] private Image artworkImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descText;

    public void Setup(CardInstance card)
    {
        if (card == null || card.baseData == null) return;
        CardSO data = card.baseData;

        if (artworkImage != null) artworkImage.sprite = data.artwork;
        if (nameText != null) nameText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(data.nameKey) : data.nameKey;
        if (typeText != null) typeText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText($"{data.cardType.ToString().ToUpper()}_KEY") : data.cardType.ToString();

        string desc = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(data.descKey) : data.descKey;
        desc = desc.Replace("[Value]", card.GetValue().ToString());
        if (descText != null) descText.text = desc;
    }
}