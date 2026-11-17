using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 휴식 씬과 게임 데이터(체력, 덱, 카드 UI)를 잇는 다리.
/// 휴식 씬 스크립트들은 이 클래스만 알고, 실제 게임 코드는 이걸 상속해 구현한다.
/// 카드는 object 로 주고받으므로 구현 쪽에서 CardInstance 등으로 캐스팅해서 쓰면 된다.
/// </summary>
public abstract class RestSceneBridge : MonoBehaviour
{
    [Tooltip("휴식 후 이동할 스테이지 선택 씬 이름 (Build Settings 에 등록돼 있어야 함)")]
    public string stageSelectScene = "StageSelectScene";

    [Tooltip("최대 체력 대비 회복 비율")]
    [Range(0f, 1f)] public float healRatio = 0.2f;

    public abstract int CurrentHP { get; }
    public abstract int MaxHP { get; }
    public virtual int HealAmount => Mathf.CeilToInt(MaxHP * healRatio);
    public int HPAfterHeal => Mathf.Min(MaxHP, CurrentHP + HealAmount);

    /// <summary>회복 적용 (컷씬의 번쩍임 순간에 호출됨)</summary>
    public abstract void ApplyHeal(int amount);

    /// <summary>스크롤뷰에 나열할 카드. forUpgrade 면 강화 가능한 카드만</summary>
    public abstract IList<object> GetCards(bool forUpgrade);

    /// <summary>
    /// 카드 UI 생성 (160 x 240 RectTransform 을 parent 아래에 만들어 반환).
    /// upgradedPreview = true 면 강화 후 모습으로 (아직 강화 적용 전, 미리보기용)
    /// </summary>
    public abstract RectTransform CreateCardView(object card, Transform parent, bool upgradedPreview);

    /// <summary>강화 적용 (컷씬의 번쩍임 순간에 호출됨)</summary>
    public abstract void ApplyUpgrade(object card);

    /// <summary>덱에서 제거 (컷씬의 폭발 순간에 호출됨)</summary>
    public abstract void RemoveCard(object card);

    /// <summary>모든 연출이 끝난 뒤. 기본은 글리치 전환으로 스테이지 선택 씬 로드</summary>
    public virtual void LeaveRest()
    {
        SceneTransition.Load(stageSelectScene);
    }
}
