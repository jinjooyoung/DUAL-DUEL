using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 드로우 덱 / 버림 덱 리스트 등에서 사용하는 UI(Canvas) 버전 카드
/// 3D 카드의 CardVisual과 같은 구조/같은 표시. 설명은 카드에 적지 않고 3D 카드와 같은 툴팁으로 띄움
/// (같은 오브젝트에 TooltipTrigger를 붙이면 isCard 체크 없이 이 컴포넌트가 툴팁 문자열을 제공)
/// </summary>
public class UIViewerCard : MonoBehaviour, ITooltipTextProvider
{
    [Header("로컬라이제이션")]
    [SerializeField] private string tableName = "Card_Table";
    [SerializeField] private LocalizeStringEvent nameLocalizeEvent;
    [SerializeField] private LocalizeStringEvent typeLocalizeEvent;

    [Header("이미지")]
    [SerializeField] private Image background;
    [FormerlySerializedAs("artworkImage")]
    [SerializeField] private Image cardResource;
    [SerializeField] private Image typeIcon;
    [SerializeField] private Image gimmickIcon;

    [Header("랭크 별 아이콘")]
    [SerializeField] private List<Image> rankStarIcons = new List<Image>();

    // 랭크 단계 수 (RankType 최댓값 = 5)
    private static readonly int MaxRankStarCount = (int)RankType.Legendary;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI gimmikValueText;

    [Header("타입, 기믹 오브젝트")]
    [SerializeField] private RectTransform typeGroup;
    [SerializeField] private GameObject gimmikGroup;

    // 툴팁 문자열을 만들 때 쓰는 현재 카드 (3D 카드의 CardDisplay.cardInstance 역할)
    private CardInstance cardInstance;

    private Vector2 originalTypeGroupPos;
    private bool isTypeGroupPosCached;

    private void Awake()
    {
        CacheTypeGroupPos();
    }

    private void CacheTypeGroupPos()
    {
        if (isTypeGroupPosCached || typeGroup == null) return;
        originalTypeGroupPos = typeGroup.anchoredPosition;
        isTypeGroupPosCached = true;
    }

    /// <summary>
    /// 기존 호출부 호환용 (덱 뷰어에서 Setup으로 호출)
    /// </summary>
    public void Setup(CardInstance card) => Render(card);

    /// <summary>
    /// 카드 전체 외형 갱신
    /// </summary>
    public void Render(CardInstance card)
    {
        cardInstance = card;
        if (card == null || card.baseData == null) return;
        CardSO data = card.baseData;

        CacheTypeGroupPos();

        // 1. 이름 / 타입 (로컬라이징 자동 번역)
        if (nameText != null && nameLocalizeEvent != null)
        {
            nameLocalizeEvent.StringReference.SetReference(tableName, data.nameKey);
            nameLocalizeEvent.RefreshString();
        }

        if (typeText != null && typeLocalizeEvent != null)
        {
            string typeKey = $"{data.cardType.ToString().ToUpper()}_KEY";
            typeLocalizeEvent.StringReference.SetReference(tableName, typeKey);
            typeLocalizeEvent.RefreshString();
        }

        // 2. 스프라이트 (예전 랭크별 배경색이 남지 않도록 색은 흰색 고정)
        if (cardResource != null && data.artwork != null) cardResource.sprite = data.artwork;
        if (background != null)
        {
            background.sprite = data.bgSprite;
            background.color = Color.white;
        }
        if (typeIcon != null) typeIcon.sprite = data.typeIconSprite;
        if (gimmickIcon != null) gimmickIcon.sprite = data.gimmikIconSprite;

        // 3. 랭크 별
        int activeStarCount = (int)data.rank;
        for (int i = 0; i < rankStarIcons.Count; i++)
        {
            if (rankStarIcons[i] == null) continue;

            bool isUsedStar = i < MaxRankStarCount;
            rankStarIcons[i].gameObject.SetActive(isUsedStar);
            if (!isUsedStar) continue;

            rankStarIcons[i].sprite = i < activeStarCount ? data.FilledStarSprite : data.EmptyStartSprite;
        }

        // 4. 기믹 그룹 노출 (보상이 없는 카드는 타입 그룹을 가운데로)
        bool hasReward = data.rewardCategory != RewardCategory.None;
        if (gimmikGroup != null) gimmikGroup.SetActive(hasReward);
        if (typeGroup != null)
        {
            typeGroup.anchoredPosition = hasReward
                ? originalTypeGroupPos
                : new Vector2(0f, originalTypeGroupPos.y);
        }

        // 5. 수치
        RefreshValues(card);
    }

    /// <summary>
    /// 강화 단계가 반영된 밸류 / 리워드 파람 텍스트만 갱신 (강화 시 호출)
    /// </summary>
    public void RefreshValues(CardInstance card)
    {
        if (card == null || card.baseData == null) return;

        if (valueText != null) valueText.text = card.GetValue().ToString();
        if (gimmikValueText != null) gimmikValueText.text = card.GetRewardDisplayText();
    }

    /// <summary>
    /// TooltipTrigger가 툴팁을 띄우기 직전에 호출. 3D 카드와 같은 "타입설명 \n 기믹설명" 문자열
    /// </summary>
    public string GetTooltipText()
    {
        return CardTooltipText.Build(cardInstance, tableName);
    }
}
