using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// 드로우 덱 / 버림 덱 리스트 등에서 사용하는 UI(Canvas) 버전 카드
/// 인게임 3D 카드(CardDisplay)와 같은 내용을 표시하고, 드래그/슬롯 배치 등 전투 상호작용은 없음
/// </summary>
public class UIViewerCard : MonoBehaviour
{
    [Header("이미지")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private Image background;
    [SerializeField] private Image typeIcon;
    [SerializeField] private Image gimmickIcon;

    [Header("랭크 별 아이콘")]
    [SerializeField] private List<Image> rankStarIcons = new List<Image>();

    // 랭크 단계 수 (RankType 최댓값 = 5)
    private static readonly int MaxRankStarCount = (int)RankType.Legendary;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI gimmikValueText;

    [Header("타입, 기믹 그룹")]
    [SerializeField] private RectTransform typeGroup;
    [SerializeField] private GameObject gimmikGroup;

    [Header("로컬라이제이션")]
    [SerializeField] private string tableName = "Card_Table";
    [SerializeField] private LocalizeStringEvent nameLocalizeEvent;
    [SerializeField] private LocalizeStringEvent typeLocalizeEvent;
    [SerializeField] private LocalizeStringEvent descLocalizeEvent;

    private Vector2 originalTypeGroupPos;
    private bool isTypeGroupPosCached = false;

    private void Awake()
    {
        CacheTypeGroupPos();
    }

    // 리스트에서 비활성 상태로 생성된 직후 Setup이 먼저 불려도 원래 위치를 잃지 않도록
    private void CacheTypeGroupPos()
    {
        if (isTypeGroupPosCached || typeGroup == null) return;
        originalTypeGroupPos = typeGroup.anchoredPosition;
        isTypeGroupPosCached = true;
    }

    public void Setup(CardInstance card)
    {
        if (card == null || card.baseData == null) return;
        CardSO data = card.baseData;

        CacheTypeGroupPos();

        // 1. 카드 이름
        if (nameText != null && nameLocalizeEvent != null)
        {
            nameLocalizeEvent.StringReference.SetReference(tableName, data.nameKey);
            nameLocalizeEvent.RefreshString();
        }

        // 2. 카드 타입
        if (typeText != null && typeLocalizeEvent != null)
        {
            string typeKey = $"{data.cardType.ToString().ToUpper()}_KEY";
            typeLocalizeEvent.StringReference.SetReference(tableName, typeKey);
            typeLocalizeEvent.RefreshString();
        }

        // 4. 이미지 (랭크는 배경색이 아니라 별로 표시하므로 배경색은 기본값 유지)
        if (artworkImage != null && data.artwork != null) artworkImage.sprite = data.artwork;

        if (background != null)
        {
            background.sprite = data.bgSprite;
            background.color = Color.white;
        }

        if (typeIcon != null) typeIcon.sprite = data.typeIconSprite;
        if (gimmickIcon != null) gimmickIcon.sprite = data.gimmikIconSprite;

        // 5. 랭크 별 (RankType 수만큼만 사용, 초과분은 숨김)
        int activeStarCount = (int)data.rank;

        for (int i = 0; i < rankStarIcons.Count; i++)
        {
            if (rankStarIcons[i] == null) continue;

            bool isUsedStar = i < MaxRankStarCount;
            rankStarIcons[i].gameObject.SetActive(isUsedStar);
            if (!isUsedStar) continue;

            rankStarIcons[i].sprite = i < activeStarCount ? data.FilledStarSprite : data.EmptyStartSprite;
        }

        // 6. 기믹 그룹 노출 여부 (보상이 없는 카드는 타입 그룹을 가운데로)
        bool hasReward = data.rewardCategory != RewardCategory.None;
        if (gimmikGroup != null) gimmikGroup.SetActive(hasReward);

        if (typeGroup != null)
        {
            typeGroup.anchoredPosition = hasReward
                ? originalTypeGroupPos
                : new Vector2(0f, originalTypeGroupPos.y);
        }

        // 7. 수치 텍스트
        if (valueText != null) valueText.text = card.GetValue().ToString();
        if (gimmikValueText != null) gimmikValueText.text = card.GetRewardDisplayText();
    }
}
