using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class UIViewerCard : MonoBehaviour
{
    [Header("카드 이미지")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private Image background;

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

        if (background != null)
        {
            switch (data.rank)
            {
                case RankType.Common:
                    background.color = Hex("#FFFFFF");
                    break;
                case RankType.Uncommon:
                    background.color = Hex("#9EE647");
                    break;
                case RankType.Rare:
                    background.color = Hex("#58A3E6");
                    break;
                case RankType.Epic:
                    background.color = Hex("#E6606E");
                    break;
                case RankType.Legendary:
                    background.color = Hex("#9071E6");
                    break;
                case RankType.Mythic:
                    background.color = Hex("#FCE66F");
                    break;
            }
        }

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
            // 1. {value} 인자 먼저 등록
            descLocalizeEvent.StringReference.Arguments = new object[]
            {
                new { value = card.GetValue() }
            };

            // 2. 그 다음 테이블/키 설정
            descLocalizeEvent.StringReference.SetReference(tableName, data.descKey);

            // 3. 텍스트 즉시 갱신
            descLocalizeEvent.RefreshString();
        }
    }

    private Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}