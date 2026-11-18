using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상점 카드 한 장 (UIViewerCard 와 같은 오브젝트에 붙음).
///   누름 -> 0.1초에 원래 크기 x0.7,  뗌 -> 원래 크기로 (구매가 일어나면 구매 연출이 이어받음)
///   구매 클릭은 Button.onClick (덱 뷰어/보상 화면과 같은 클릭 인식)
///   살 수 없으면 Button.interactable = false + 카드 위에 검은 막을 덮어 어둡게
/// </summary>
public class ShopCardSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Button Button { get; private set; }
    public Vector3 OriginalScale { get; private set; }

    [SerializeField] private float pressScale = 0.7f;
    [SerializeField] private float pressTime = 0.1f;

    private Image dimOverlay;
    private bool isSold;
    private bool isInitialized;

    public void Init(UnityAction onClick)
    {
        if (!isInitialized)
        {
            OriginalScale = transform.localScale;

            // 클릭을 받을 그래픽이 루트에 없으면 투명 이미지 추가
            Graphic g = GetComponent<Graphic>();
            if (g == null) { g = gameObject.AddComponent<Image>(); g.color = new Color(0f, 0f, 0f, 0f); }
            g.raycastTarget = true;

            // 프리팹에 버튼이 있으면 그대로 쓰고 (호버 효과 유지), 없을 때만 추가
            Button = GetComponent<Button>();
            if (Button == null)
            {
                Button = gameObject.AddComponent<Button>();
                Button.transition = Selectable.Transition.None;
            }

            // 어둡게 덮는 막 (맨 위, 클릭은 통과)
            var go = new GameObject("ShopDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            dimOverlay = go.GetComponent<Image>();
            dimOverlay.raycastTarget = false;
            dimOverlay.color = new Color(0f, 0f, 0f, 0f);
            isInitialized = true;
        }
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(onClick);
    }

    /// <summary>살 수 있는지 반영. brightness = 살 수 없을 때의 밝기 (0.4 = 원래의 40%)</summary>
    public void SetAffordable(bool canBuy, float brightness)
    {
        if (isSold || Button == null) return;
        Button.interactable = canBuy;
        if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, canBuy ? 0f : 1f - brightness);
    }

    /// <summary>구매됨: 더 이상 클릭/누름 연출을 받지 않음 (크기 연출은 구매 연출이 맡음)</summary>
    public void SetSold()
    {
        isSold = true;
        if (Button != null) Button.interactable = false;
        if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, 0f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isSold || Button == null || !Button.interactable) return;
        DOTween.Kill(transform);
        transform.DOScale(OriginalScale * pressScale, pressTime).SetEase(Ease.OutQuad).SetTarget(transform);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 카드 밖에서 떼어 구매가 안 된 경우 원래 크기로. 구매되면 같은 프레임의 onClick 에서 구매 연출이 이 트윈을 끊고 이어받음
        if (isSold) return;
        DOTween.Kill(transform);
        transform.DOScale(OriginalScale, pressTime).SetEase(Ease.OutQuad).SetTarget(transform);
    }
}
