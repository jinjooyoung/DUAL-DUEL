using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class UIViewerCard : MonoBehaviour
{
    [Header("카드 이미지")]
    [SerializeField] private Image artworkImage;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descText;

    [Header("로컬라이제이션")]
    [SerializeField] private string tableName = "Card_Table";
    [SerializeField] private LocalizeStringEvent nameLocalizeEvent;
    [SerializeField] private LocalizeStringEvent typeLocalizeEvent;
    [SerializeField] private LocalizeStringEvent descLocalizeEvent;

    public void Setup(CardInstance card)
    {
        if (card == null || card.baseData == null) return;
        CardSO data = card.baseData;

        // 카드 이미지
        if (artworkImage != null) artworkImage.sprite = data.artwork;

        // 카드 이름
        if (nameText != null && nameLocalizeEvent != null)
        {
            nameLocalizeEvent.StringReference.SetReference(tableName, data.nameKey);

            nameLocalizeEvent.RefreshString();
        }

        // 카드 타입
        if (typeText != null && typeLocalizeEvent != null)
        {
            string typeKey = $"{data.cardType.ToString().ToUpper()}_KEY";

            typeLocalizeEvent.StringReference.SetReference(tableName, typeKey);

            typeLocalizeEvent.RefreshString();
        }

        // 카드 설명 및 수치 인자
        if (descText != null && descLocalizeEvent != null)
        {
            descLocalizeEvent.StringReference.SetReference(tableName, data.descKey);

            descLocalizeEvent.StringReference.Arguments = new object[]
                {
                    new { value = card.GetValue() }
                };

            descLocalizeEvent.RefreshString();
        }
    }
}