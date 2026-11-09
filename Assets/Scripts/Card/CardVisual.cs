using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

/// <summary>
/// 3D 카드의 표시 전담 (이름/타입 로컬라이징, 스프라이트, 랭크 별, 수치 텍스트)
/// 데이터 보관과 드래그/배치 조작은 CardDisplay가 담당하고, 화면 갱신은 이 컴포넌트에 위임
/// </summary>
public class CardVisual : MonoBehaviour
{
    [Header("로컬라이제이션")]
    [SerializeField] private string tableName = "Card_Table";
    [SerializeField] private LocalizeStringEvent nameLocalizeEvent;
    [SerializeField] private LocalizeStringEvent typeLocalizeEvent;

    [Header("이미지")]
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private SpriteRenderer cardResource;
    [SerializeField] private SpriteRenderer typeIcon;
    [SerializeField] private SpriteRenderer gimmickIcon;

    [Header("랭크 별 아이콘")]
    [SerializeField] private List<SpriteRenderer> rankStarIcons = new List<SpriteRenderer>();

    // 랭크 단계 수 (RankType 최댓값 = 5)
    private static readonly int MaxRankStarCount = (int)RankType.Legendary;

    [Header("텍스트")]
    [SerializeField] private TextMeshPro nameText;
    [SerializeField] private TextMeshPro typeText;
    [SerializeField] private TextMeshPro valueText;
    [SerializeField] private TextMeshPro gimmikValueText;

    [Header("타입, 기믹 오브젝트")]
    [SerializeField] private GameObject typeGroup;
    [SerializeField] private GameObject gimmikGroup;

    private Vector3 originalTypeGroupPos;
    private bool isTypeGroupPosCached;

    private void Awake()
    {
        CacheTypeGroupPos();
    }

    private void CacheTypeGroupPos()
    {
        if (isTypeGroupPosCached || typeGroup == null) return;
        originalTypeGroupPos = typeGroup.transform.localPosition;
        isTypeGroupPosCached = true;
    }

    /// <summary>
    /// 카드 전체 외형 갱신
    /// </summary>
    public void Render(CardInstance card)
    {
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

        // 2. 스프라이트
        if (cardResource != null && data.artwork != null) cardResource.sprite = data.artwork;
        if (background != null) background.sprite = data.bgSprite;
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
            typeGroup.transform.localPosition = hasReward
                ? originalTypeGroupPos
                : new Vector3(0, originalTypeGroupPos.y, originalTypeGroupPos.z);
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
}
