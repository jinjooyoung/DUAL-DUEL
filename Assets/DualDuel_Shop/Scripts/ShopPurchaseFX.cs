using DG.Tweening;
using UnityEngine;

/// <summary>
/// 상점 구매 연출: 띠용 -> 빙글빙글 -> 번쩍 -> 덱으로
///   0.0  ~ 0.1  원래 크기 x0.9 (누름 x0.7 에서 튕김, 원본보다 작게)
///   0.1  ~ 1.1  두 바퀴(720°) 돌며 x0.1 로 (회전은 빠르게 시작해 느려지고, 축소는 천천히 시작해 빨라짐)
///   0.8         회전 0.7초: 스파클 + 원형 빛이 카드 높이만큼 번쩍 -> 0.3초 동안 카드와 함께 사그라듦
///   0.8  ~ 1.1  빛줄기가 카드에서 위로 살짝 떴다가 포물선으로 덱(좌하단)까지 날아감 -> 도착 지점에서 작게 반짝
/// 빛은 월드 스프라이트 / 라인렌더러라 HDR 색으로 블룸을 받음 (정렬 160: UI 위, CRT 스캔라인 200 아래)
/// </summary>
public class ShopPurchaseFX : MonoBehaviour
{
    [Header("스프라이트 (상점 씬 구성 툴이 채움)")]
    public Sprite sparkleSprite;   // FX_Shop_Sparkle (변이 움푹 팬 네 꼭짓점 별)
    public Sprite glowSprite;      // FX_Shop_Glow (원형 빛)

    [Header("시간")]
    public float bounceScale = 0.9f;
    public float bounceTime = 0.1f;
    public float spinTime = 1f;
    [Tooltip("회전 시작 후 번쩍 + 빛줄기 출발 시점")] public float flashAt = 0.7f;
    [Tooltip("번쩍 후 사그라드는 시간")] public float flashFade = 0.3f;
    [Tooltip("빛줄기가 덱까지 날아가는 시간")] public float trailTime = 0.3f;

    [Header("빛")]
    [ColorUsage(false, true)] public Color lightColor = new Color(1.6f, 2.4f, 1.7f, 1f);
    [Tooltip("빛줄기 머리 두께 (월드 단위)")] public float trailWidth = 0.12f;
    [Tooltip("빛줄기가 위로 떠오르는 높이 (월드 단위)")] public float trailArc = 1.6f;
    [Range(0.05f, 0.9f)] public float trailTail = 0.35f;
    public int sortingOrder = 160;

    Material _material;

    /// <summary>card 의 구매 연출 재생. originalScale = 카드의 원래 크기 (누르기 전)</summary>
    public void Play(RectTransform card, Vector3 originalScale, RectTransform deck)
    {
        if (card == null) return;
        DOTween.Kill(card);   // 누름/뗌 트윈을 끊고 이어받음

        var group = card.GetComponent<CanvasGroup>();
        if (group == null) group = card.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        // 카드 높이 (월드): 지금은 눌린 크기라 원래 크기 기준으로 환산
        float scaleNow = Mathf.Max(0.0001f, card.localScale.y);
        float cardHeight = card.rect.height * card.lossyScale.y / scaleNow * originalScale.y;

        Sequence seq = DOTween.Sequence().SetTarget(card);
        seq.Append(card.DOScale(originalScale * bounceScale, bounceTime).SetEase(Ease.OutQuad));
        seq.Append(card.DOScale(originalScale * 0.1f, spinTime).SetEase(Ease.InQuad));
        seq.Join(card.DOLocalRotate(new Vector3(0f, 0f, 720f), spinTime, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

        // Insert 는 모든 Append 뒤에 (먼저 넣으면 시퀀스 끝이 늘어나 뒤의 Append 가 밀림)
        float flashTime = bounceTime + flashAt;
        seq.InsertCallback(flashTime, () =>
        {
            Flash(card.position, cardHeight);
            if (deck != null) Trail(card.position, deck.position);
        });
        seq.Insert(flashTime, group.DOFade(0f, flashFade).SetEase(Ease.InQuad));
        seq.OnComplete(() => { if (card != null) card.gameObject.SetActive(false); });
    }

    // ------------------------------------------------------------------ 번쩍

    void Flash(Vector3 at, float size)
    {
        // 원형 빛 (카드 높이의 1.3배) + 스파클 (카드 높이): 0.04초에 커지고 나머지 동안 사그라듦
        Pop(glowSprite, at, size * 1.3f, 0.85f);
        Pop(sparkleSprite, at, size, 1f);
    }

    void Pop(Sprite sprite, Vector3 at, float size, float alpha)
    {
        if (sprite == null) return;
        SpriteRenderer sr = MakeSprite(sprite, at);
        float unit = Mathf.Max(0.0001f, sprite.bounds.size.y);
        Vector3 full = Vector3.one * (size / unit);
        sr.transform.localScale = full * 0.2f;
        sr.color = new Color(1f, 1f, 1f, alpha);
        float grow = Mathf.Min(0.04f, flashFade * 0.3f);
        DOTween.Sequence().SetLink(sr.gameObject)
            .Append(sr.transform.DOScale(full, grow).SetEase(Ease.OutQuad))
            .Append(sr.DOFade(0f, flashFade - grow).SetEase(Ease.InQuad))
            .Join(sr.transform.DOScale(full * 0.85f, flashFade - grow))
            .OnComplete(() => Destroy(sr.gameObject));
    }

    // ------------------------------------------------------------------ 덱으로 가는 빛줄기

    void Trail(Vector3 from, Vector3 to)
    {
        Vector3 toCam = Camera.main != null ? -Camera.main.transform.forward * 0.3f : Vector3.zero;
        from += toCam; to += toCam;
        // 포물선: 카드 위로 살짝 떴다가 덱으로 (2차 베지어)
        Vector3 ctrl = new Vector3(Mathf.Lerp(from.x, to.x, 0.15f), Mathf.Max(from.y, to.y) + trailArc, from.z);

        var go = new GameObject("DeckTrail");
        var lr = go.AddComponent<LineRenderer>();
        const int N = 24;
        lr.useWorldSpace = true;
        lr.positionCount = N + 1;
        lr.numCapVertices = 2;
        lr.alignment = LineAlignment.View;
        lr.sortingOrder = sortingOrder;
        lr.sharedMaterial = Material();
        lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 1f));
        lr.widthMultiplier = trailWidth;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 1f) });
        lr.colorGradient = grad;

        SpriteRenderer head = glowSprite != null ? MakeSprite(glowSprite, from) : null;
        float headUnit = glowSprite != null ? Mathf.Max(0.0001f, glowSprite.bounds.size.y) : 1f;
        if (head != null) head.transform.localScale = Vector3.one * (trailWidth * 4.5f / headUnit);

        void Draw(float tail, float headT)
        {
            for (int i = 0; i <= N; i++) lr.SetPosition(i, Bezier(from, ctrl, to, Mathf.Lerp(tail, headT, i / (float)N)));
            if (head != null) head.transform.position = Bezier(from, ctrl, to, headT);
        }
        Draw(0f, 0f);

        Sequence seq = DOTween.Sequence().SetLink(go);
        // 날아감: 출발은 부드럽게, 도착할 때 빨려 들어가듯 빨라짐
        seq.Append(DOVirtual.Float(0f, 1f, trailTime, u =>
        {
            float s = u * u * (3f - 2f * u) * 0.4f + u * u * 0.6f;
            Draw(Mathf.Max(0f, s - trailTail), s);
        }).SetEase(Ease.Linear));
        // 도착: 덱 자리에서 작게 반짝, 꼬리는 머리로 빨려 들어감
        seq.AppendCallback(() =>
        {
            if (head != null) Destroy(head.gameObject);
            Pop(glowSprite, to - toCam, trailWidth * 10f, 0.9f);
            Pop(sparkleSprite, to - toCam, trailWidth * 6f, 1f);
        });
        seq.Append(DOVirtual.Float(1f - trailTail, 1f, 0.1f, tail => Draw(tail, 1f)));
        seq.OnComplete(() => Destroy(go));
    }

    static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float m = 1f - t;
        return m * m * a + 2f * m * t * b + t * t * c;
    }

    // ------------------------------------------------------------------ helpers

    SpriteRenderer MakeSprite(Sprite sprite, Vector3 at)
    {
        var go = new GameObject("ShopFX");
        go.transform.position = at + (Camera.main != null ? -Camera.main.transform.forward * 0.3f : Vector3.zero);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = Material();
        sr.sortingOrder = sortingOrder + 1;
        return sr;
    }

    Material Material()
    {
        if (_material != null) return _material;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        _material = new Material(sh) { name = "ShopFX (runtime)", color = lightColor };   // HDR 이라 블룸을 받음
        return _material;
    }

    void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
