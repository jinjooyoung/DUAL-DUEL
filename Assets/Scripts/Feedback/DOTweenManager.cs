using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 범용 정적 트윈 매니저
/// 3D 오브젝트(카드, 슬롯)와 Canvas UI등 모든 오브젝트 표준 피드백 연출
/// </summary>
public static class DOTweenManager
{
    private static readonly int ColorPropID = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropID = Shader.PropertyToID("_BaseColor");

    // Tween ID 접미사 상수 (트윈 채널 충돌 방지)
    private const string ID_TRANSFORM = "_tr";
    private const string ID_COLOR = "_col";
    private const string ID_CARD_STATE = "_card";

    #region 1. 기본 Transform 효과 (Transform Core)

    /// <summary>
    /// 대상의 Scale을 지정된 비율(또는 절대값)로 변경합니다.
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="targetScale">목표 Scale (절대 크기)</param>
    /// <param name="duration">진행 시간</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween Scale(Transform target, Vector3 targetScale, float duration = 0.15f, Ease ease = Ease.OutQuad)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return target.DOScale(targetScale, duration).SetEase(ease).SetId(tweenId);
    }

    /// <summary>
    /// 대상의 현재 Scale에 배율(Multiplier)을 곱하여 스케일을 변경합니다.
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="multiplier">크기 배율 (예: 1.2f = 120% 확대)</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween ScaleMultiplier(Transform target, float multiplier, float duration = 0.15f, Ease ease = Ease.OutQuad)
    {
        if (target == null) return null;
        return Scale(target, target.localScale * multiplier, duration, ease);
    }

    /// <summary>
    /// 대상의 크기를 일시적으로 튕기듯 키웠다가 원래 크기로 되돌립니다.
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="punchStrength">튕길 변형 강도</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="vibrato">진동 횟수</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween PunchScale(Transform target, float punchStrength = 0.15f, float duration = 0.2f, int vibrato = 5, Ease ease = Ease.OutQuad)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return target.DOPunchScale(Vector3.one * punchStrength, duration, vibrato, 0.5f).SetEase(ease).SetId(tweenId);
    }

    /// <summary>
    /// 대상을 지정한 월드 좌표로 이동시킵니다.
    /// </summary>
    ///<param name="target">대상 Transform</param>
    /// <param name="targetPosition">목표 월드 좌표</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween Move(Transform target, Vector3 targetPosition, float duration = 0.2f, Ease ease = Ease.OutQuad)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return target.DOMove(targetPosition, duration).SetEase(ease).SetId(tweenId);
    }

    /// <summary>
    /// 현재 위치를 기준으로 상대적(오프셋) 방향으로 이동시킵니다.
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="deltaOffset">현재 위치에서의 상대적 이동 offset 벡터</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween MoveRelative(Transform target, Vector3 deltaOffset, float duration = 0.2f, Ease ease = Ease.OutQuad)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return target.DOMove(target.position + deltaOffset, duration).SetEase(ease).SetId(tweenId);
    }

    /// <summary>
    /// 대상의 회전값을 오일러 각도 기준으로 부드럽게 변경합니다.
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="targetEulerAngles">목표 회전 각도 (Euler Vector3)</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween Rotate(Transform target, Vector3 targetEulerAngles, float duration = 0.2f, Ease ease = Ease.OutQuad)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return target.DORotate(targetEulerAngles, duration).SetEase(ease).SetId(tweenId);
    }

    /// <summary>
    /// 대상의 회전축을 순간적으로 튕기듯 흔듭니다. (잘못된 슬롯 배치나 피격 연출 등)
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="punchStrength">각 축별 회전 강도</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="vibrato">진동 횟수</param>
    public static Tween PunchRotation(Transform target, Vector3 punchStrength, float duration = 0.25f, int vibrato = 6)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return target.DOPunchRotation(punchStrength, duration, vibrato).SetId(tweenId);
    }

    /// <summary>
    /// 대상의 위치를 짧게 진동시킵니다.
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="strength">진동 강도</param>
    /// <param name="vibrato">진동 횟수</param>
    /// <param name="randomness">진동의 무작위도 (각도 범위)</param>
    public static Tween ShakePosition(Transform target, float duration = 0.25f, float strength = 0.15f, int vibrato = 10, float randomness = 90f)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return target.DOShakePosition(duration, strength, vibrato, randomness).SetId(tweenId);
    }

    /// <summary>
    /// 대상의 회전값을 짧게 진동시킵니다.
    /// </summary>
    /// <param name="target">대상 Transform</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="strength">회전 진동 강도</param>
    /// <param name="vibrato">진동 횟수</param>
    public static Tween ShakeRotation(Transform target, float duration = 0.25f, float strength = 10f, int vibrato = 10)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return target.DOShakeRotation(duration, strength, vibrato).SetId(tweenId);
    }

    #endregion

    #region 2. Fade / Alpha / Flash 효과

    /// <summary>
    /// CanvasGroup, Graphic(UI), SpriteRenderer, MeshRenderer를 자동으로 판별하여 알파를 변경합니다.
    /// </summary>
    /// <param name="target">대상 GameObject</param>
    /// <param name="targetAlpha">목표 알파값 (0.0 ~ 1.0)</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween Fade(GameObject target, float targetAlpha, float duration = 0.2f, Ease ease = Ease.Linear)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_COLOR;
        DOTween.Kill(tweenId);

        // 1. CanvasGroup 검사
        if (target.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            return canvasGroup.DOFade(targetAlpha, duration).SetEase(ease).SetId(tweenId);
        }

        // 2. 2D UI Graphic 검사 (Image, TMP_Text 등)
        if (target.TryGetComponent<Graphic>(out var graphic))
        {
            return graphic.DOFade(targetAlpha, duration).SetEase(ease).SetId(tweenId);
        }

        // 3. 2D/3D Quad SpriteRenderer 검사
        if (target.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            return spriteRenderer.DOFade(targetAlpha, duration).SetEase(ease).SetId(tweenId);
        }

        // 4. 3D MeshRenderer (MaterialPropertyBlock 활용으로 런타임 머티리얼 복제 방지)
        if (target.TryGetComponent<Renderer>(out var meshRenderer))
        {
            var mpb = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(mpb);
            int propId = meshRenderer.sharedMaterial.HasProperty(BaseColorPropID) ? BaseColorPropID : ColorPropID;
            Color curColor = meshRenderer.sharedMaterial.HasProperty(propId) ? meshRenderer.sharedMaterial.GetColor(propId) : Color.white;

            return DOTween.To(() => curColor.a, a =>
            {
                if (meshRenderer == null) return;
                meshRenderer.GetPropertyBlock(mpb);
                curColor.a = a;
                mpb.SetColor(propId, curColor);
                meshRenderer.SetPropertyBlock(mpb);
            }, targetAlpha, duration).SetEase(ease).SetId(tweenId);
        }

        return null;
    }

    public static Tween FadeIn(GameObject target, float duration = 0.2f, Ease ease = Ease.OutQuad) => Fade(target, 1f, duration, ease);
    public static Tween FadeOut(GameObject target, float duration = 0.2f, Ease ease = Ease.InQuad) => Fade(target, 0f, duration, ease);

    /// <summary>
    /// 대상의 색상을 잠시 특정 색으로 번쩍인 뒤 원래 색상으로 되돌립니다.
    /// </summary>
    /// <param name="target">대상 GameObject</param>
    /// <param name="flashColor">번쩍일 목표 색상</param>
    /// <param name="duration">전체 플래시 진행 시간 (초)</param>
    /// <param name="flashCount">반복 깜빡임 횟수</param>
    public static Sequence Flash(GameObject target, Color flashColor, float duration = 0.2f, int flashCount = 1)
    {
        if (target == null) return null;
        string tweenId = target.GetInstanceID() + ID_COLOR;
        DOTween.Kill(tweenId);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        float stepDuration = duration / (flashCount * 2f);

        if (target.TryGetComponent<Graphic>(out var graphic))
        {
            Color originColor = graphic.color;
            for (int i = 0; i < flashCount; i++)
            {
                seq.Append(graphic.DOColor(flashColor, stepDuration));
                seq.Append(graphic.DOColor(originColor, stepDuration));
            }
        }
        else if (target.TryGetComponent<SpriteRenderer>(out var sr))
        {
            Color originColor = sr.color;
            for (int i = 0; i < flashCount; i++)
            {
                seq.Append(sr.DOColor(flashColor, stepDuration));
                seq.Append(sr.DOColor(originColor, stepDuration));
            }
        }
        else if (target.TryGetComponent<Renderer>(out var r))
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            int propId = r.sharedMaterial.HasProperty(BaseColorPropID) ? BaseColorPropID : ColorPropID;
            Color originColor = r.sharedMaterial.HasProperty(propId) ? r.sharedMaterial.GetColor(propId) : Color.white;

            Action<Color> applyColor = (c) =>
            {
                if (r == null) return;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(propId, c);
                r.SetPropertyBlock(mpb);
            };

            for (int i = 0; i < flashCount; i++)
            {
                Color current = originColor;
                seq.Append(DOTween.To(() => current, c => { current = c; applyColor(c); }, flashColor, stepDuration));
                seq.Append(DOTween.To(() => current, c => { current = c; applyColor(c); }, originColor, stepDuration));
            }
        }

        return seq;
    }

    #endregion

    #region 3. Card 전용 효과 (Card Lifecycle)

    /// <summary>
    /// 카드 위에 마우스를 올렸을 때: 확대 및 상승 후 그 위치 기준으로 부드럽게 위아래로 둥실거립니다.
    /// </summary>
    /// <param name="card">대상 카드 Transform</param>
    /// <param name="originPos">카드 원본 위치</param>
    /// <param name="originScale">카드 원본 크기</param>
    /// <param name="scaleMultiplier">확대 배율</param>
    /// <param name="moveY">최초 상승 높이</param>
    /// <param name="duration">진입 시간 (초)</param>
    /// <param name="floatDistance">공중에서 둥실거릴 위아래 왕복 진폭</param>
    /// <param name="floatCycleTime">둥실거리는 1주기 시간 (초)</param>
    public static Sequence CardHover(
        Transform card,
        Vector3 originPos,
        Vector3 originScale,
        float scaleMultiplier = 1.08f,
        float moveY = 0.05f,
        float duration = 0.12f,
        float floatDistance = 0.015f,
        float floatCycleTime = 0.8f)
    {
        if (card == null) return null;

        string tweenId = card.GetInstanceID() + ID_CARD_STATE;
        DOTween.Kill(tweenId);

        Vector3 hoverBasePos = originPos + new Vector3(0f, moveY, -0.02f);
        Vector3 hoverTopPos = hoverBasePos + new Vector3(0f, floatDistance, 0f);

        Sequence seq = DOTween.Sequence().SetId(tweenId);

        // 1단계: 마우스 진입 시 목표 공중 위치와 목표 크기로 빠르게 안착
        seq.Append(card.DOMove(hoverBasePos, duration).SetEase(Ease.OutQuad));
        seq.Join(card.DOScale(originScale * scaleMultiplier, duration).SetEase(Ease.OutQuad));

        // 2단계: 안착된 공중 위치에서 무한 루프로 둥실거리기 (InOutSine으로 부드러운 물결 느낌)
        seq.Append(card.DOMove(hoverTopPos, floatCycleTime * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo));

        return seq;
    }

    /// <summary>
    /// 카드 Hover 종료: 펀치나 흔들림 없이 원본 위치, 스케일, 회전값으로 부드럽게 복귀시킵니다.
    /// </summary>
    /// <param name="card">대상 카드 Transform</param>
    /// <param name="originPos">복귀할 원본 위치</param>
    /// <param name="originScale">복귀할 원본 스케일</param>
    /// <param name="originEuler">복귀할 원본 회전값 (Euler)</param>
    /// <param name="duration">복귀 진행 시간 (초)</param>
    public static Sequence CardHoverExit(
        Transform card,
        Vector3 originPos,
        Vector3 originScale,
        Vector3 originEuler,
        float duration = 0.12f)
    {
        if (card == null) return null;

        string tweenId = card.GetInstanceID() + ID_CARD_STATE;
        // Hover에서 돌고 있던 무한 루프 Tween을 즉시 중단
        DOTween.Kill(tweenId);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        seq.Join(card.DOMove(originPos, duration).SetEase(Ease.OutQuad));
        seq.Join(card.DOScale(originScale, duration).SetEase(Ease.OutQuad));
        seq.Join(card.DORotate(originEuler, duration).SetEase(Ease.OutQuad));

        return seq;
    }

    /// <summary>
    /// 카드를 클릭하여 선택했을 때 확실히 부각시키는 연출입니다.
    /// </summary>
    /// <param name="card">대상 카드 Transform</param>
    /// <param name="originPos">카드 기준 원본 위치</param>
    /// <param name="originScale">카드 기준 원본 스케일</param>
    /// <param name="scaleMult">선택 시 확대 배율</param>
    /// <param name="liftY">선택 시 Y축 돌출 높이</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence CardSelect(Transform card, Vector3 originPos, Vector3 originScale, float scaleMult = 1.15f, float liftY = 0.12f, float duration = 0.15f)
    {
        if (card == null) return null;
        string tweenId = card.GetInstanceID() + ID_CARD_STATE;
        DOTween.Kill(tweenId);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        seq.Join(card.DOMove(originPos + new Vector3(0f, liftY, -0.05f), duration).SetEase(Ease.OutBack));
        seq.Join(card.DOScale(originScale * scaleMult, duration).SetEase(Ease.OutBack));
        return seq;
    }

    /// <summary>
    /// 카드 드래그 시작 피드백 (손가락/마우스에 따라오기 전 살짝 부유).
    /// </summary>
    /// <param name="card">대상 카드 Transform</param>
    /// <param name="dragScale">드래그 중 적용할 스케일 Vector3</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Tween CardDragStart(Transform card, Vector3 dragScale, float duration = 0.1f)
    {
        if (card == null) return null;
        string tweenId = card.GetInstanceID() + ID_CARD_STATE;
        DOTween.Kill(tweenId);

        return card.DOScale(dragScale, duration).SetEase(Ease.OutQuad).SetId(tweenId);
    }

    /// <summary>
    /// 카드 드래그 종료 후 원위치 또는 슬롯 안착 전 스케일 복귀.
    /// </summary>
    /// <param name="card">대상 카드 Transform</param>
    /// <param name="targetScale">복귀할 목표 스케일 Vector3</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence CardDragEnd(Transform card, Vector3 targetScale, float duration = 0.1f, Action onComplete = null)
    {
        if (card == null) return null;
        string tweenId = card.GetInstanceID() + ID_CARD_STATE;
        DOTween.Kill(tweenId);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        seq.Append(card.DOScale(targetScale, duration).SetEase(Ease.OutQuad));
        seq.OnComplete(() => onComplete?.Invoke());
        return seq;
    }

    /// <summary>
    /// 카드가 슬롯에 장착될 때의 연출입니다. 슬롯으로 자석처럼 이동 후 임팩트 펀치를 줍니다.
    /// </summary>
    /// <param name="card">대상 카드 Transform</param>
    /// <param name="slotPosition">슬롯 안착 월드 위치</param>
    /// <param name="slotRotation">슬롯 안착 오일러 회전값</param>
    /// <param name="targetScale">슬롯 안착 목표 스케일</param>
    /// <param name="duration">이동 진행 시간 (초)</param>
    /// <param name="onComplete">장착 완료 시 실행할 콜백 메서드</param>
    public static Sequence CardPlace(Transform card, Vector3 slotPosition, Vector3 slotRotation, Vector3 targetScale, float duration = 0.18f, Action onComplete = null)
    {
        if (card == null) return null;
        string tweenId = card.GetInstanceID() + ID_CARD_STATE;
        DOTween.Kill(tweenId);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        seq.Append(card.DOMove(slotPosition, duration).SetEase(Ease.OutCubic));
        seq.Join(card.DORotate(slotRotation, duration).SetEase(Ease.OutCubic));
        seq.Join(card.DOScale(targetScale * 0.9f, duration).SetEase(Ease.OutCubic));
        seq.Append(card.DOScale(targetScale, 0.2f).SetEase(Ease.OutQuint));
        seq.OnComplete(() => onComplete?.Invoke());
        return seq;
    }

    /// <summary>
    /// 슬롯에 배치된 카드가 효과를 실행할 때 앞으로 돌출되었다가 복귀하는 연출입니다.
    /// </summary>
    /// <param name="card">대상 카드 Transform</param>
    /// <param name="forwardDir">전진 돌출할 방향 벡터</param>
    /// <param name="advanceDist">전진 돌출 거리</param>
    /// <param name="duration">전체 연출 지속 시간 (초)</param>
    /// <param name="onHitMoment">전진 최대 지점(타격 시점)에 호출될 콜백</param>
    public static Sequence CardExecute(Transform card, Vector3 forwardDir, float advanceDist = 0.2f, float duration = 0.22f, Action onHitMoment = null)
    {
        if (card == null) return null;
        string tweenId = card.GetInstanceID() + ID_CARD_STATE;
        DOTween.Kill(tweenId);

        Vector3 originalPos = card.position;
        Vector3 targetForwardPos = originalPos + (forwardDir.normalized * advanceDist);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        // 1. 스케일 펀치
        seq.Append(card.DOPunchScale(Vector3.one * 0.15f, duration * 0.4f, 5));
        // 2. 복귀
        seq.Append(card.DOMove(originalPos, duration * 0.6f).SetEase(Ease.InOutQuad));
        return seq;
    }

    /// <summary>
    /// 덱에서 손패로 카드가 날아와 안착하는 드로우 연출입니다.
    /// </summary>
    /// <param name="card">대상 카드 Transform</param>
    /// <param name="startPos">드로우 시작 위치 (덱 위치)</param>
    /// <param name="targetPos">손패 안착 목표 위치</param>
    /// <param name="targetScale">손패 안착 목표 스케일</param>
    /// <param name="duration">이동 진행 시간 (초)</param>
    /// <param name="delay">드로우 시작 전 지연 시간 (초)</param>
    public static Sequence CardDraw(Transform card, Vector3 startPos, Vector3 targetPos, Vector3 targetScale, float duration = 0.3f, float delay = 0f)
    {
        if (card == null) return null;
        string tweenId = card.GetInstanceID() + ID_CARD_STATE;
        DOTween.Kill(tweenId);

        card.position = startPos;
        card.localScale = targetScale * 0.5f;
        
        // 카드 버릴때 Fade 해서 투명해진거 복구
        //Fade(card.gameObject, 1f, 0.25f);

        Sequence seq = DOTween.Sequence().SetId(tweenId).SetDelay(delay);
        seq.Append(card.DOMove(targetPos, duration).SetEase(Ease.OutCubic));
        seq.Join(card.DOScale(targetScale, duration).SetEase(Ease.OutBack));
        return seq;
    }

    /// <summary>
    /// 카드가 묘지나 소멸 슬롯으로 사라질 때 축소되며 페이드아웃되는 연출입니다.
    /// </summary>
    /// <param name="cardObj">대상 카드 GameObject</param>
    /// <param name="discardPos">버려질 목표 위치 (묘지 등)</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="onComplete">소멸 연출 완료 후 실행할 콜백</param>
    public static Sequence CardDiscard(GameObject cardObj, Vector3 discardPos, float duration = 0.25f, Action onComplete = null)
    {
        if (cardObj == null) return null;
        string tweenId = cardObj.GetInstanceID() + ID_CARD_STATE;
        DOTween.Kill(tweenId);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        seq.Append(cardObj.transform.DOMove(discardPos, duration).SetEase(Ease.InQuad));
        seq.Join(cardObj.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad));
        //seq.Join(Fade(cardObj, 0f, duration, Ease.InQuad));
        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            cardObj.SetActive(false);
        });
        return seq;
    }

    #endregion

    #region 4. Slot 전용 효과 (Slot Feedback)

    /// <summary>
    /// 마우스가 슬롯에 진입했을 때의 가벼운 하이라이트 스케일.
    /// </summary>
    /// <param name="slot">대상 슬롯 Transform</param>
    /// <param name="scaleMultiplier">하이라이트 스케일 배율</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Tween SlotHover(Transform slot, Vector3 originScale, float scaleMultiplier = 1.05f, float duration = 0.1f)
    {
        if (slot == null) return null;
        return Scale(slot, originScale * scaleMultiplier, duration, Ease.OutQuad);
    }

    /// <summary>
    /// 슬롯 호버 종료 시 원본 크기로 복귀
    /// </summary>
    /// <param name="slot">대상 슬롯 Transform</param>
    /// <param name="scaleMultiplier">하이라이트 스케일 배율</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Tween SlotHoverExit(Transform slot, Vector3 originScale, float duration = 0.1f)
    {
        if (slot == null) return null;
        return Scale(slot, originScale, duration, Ease.OutQuad);
    }

    /// <summary>
    /// 카드 배치가 유효한 슬롯임을 초록색/밝은 플래시로 알립니다.
    /// </summary>
    /// <param name="slot">대상 슬롯 GameObject</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence SlotValid(GameObject slot, float duration = 0.2f)
    {
        return Flash(slot, new Color(0.2f, 1f, 0.3f, 1f), duration, 1);
    }

    /// <summary>
    /// 카드를 놓을 수 없는 상태임을 붉은색 플래시 및 흔들림으로 알립니다.
    /// </summary>
    /// <param name="slot">대상 슬롯 GameObject</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence SlotInvalid(GameObject slot, float duration = 0.25f)
    {
        if (slot == null) return null;
        Sequence seq = DOTween.Sequence();
        seq.Join(Flash(slot, new Color(1f, 0.2f, 0.2f, 1f), duration, 1));
        seq.Join(PunchRotation(slot.transform, new Vector3(0f, 0f, 12f), duration, 8));
        return seq;
    }

    /// <summary>
    /// 슬롯에 카드가 장착되었을 때 슬롯 자체의 반응 펀치.
    /// </summary>
    /// <param name="slot">대상 슬롯 Transform</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Tween SlotCardPlaced(Transform slot, float duration = 0.15f)
    {
        return PunchScale(slot, 0.08f, duration, 5);
    }

    #endregion

    #region 5. Combat / Floating Numbers (전투 피드백)

    /// <summary>
    /// 플로팅 텍스트(데미지, 힐, 버프 등)의 공통 연출 코어입니다.
    /// 숫자가 팍 튀어나오며 위로 떠오르고 페이드아웃됩니다.
    /// </summary>
    /// <param name="textComponent">표시할 TextMeshPro 컴포넌트</param>
    /// <param name="content">출력할 문자열 내용</param>
    /// <param name="textColor">텍스트 색상</param>
    /// <param name="direction">떠오를 이동 방향 벡터</param>
    /// <param name="distance">이동 거리</param>
    /// <param name="duration">전체 연출 지속 시간 (초)</param>
    /// <param name="startScale">시작 스케일 크기 (팝업 임팩트용)</param>
    public static Sequence FloatingText(TMP_Text textComponent, string content, Color textColor, Vector3 direction, float distance = 0.5f, float duration = 0.6f, float startScale = 1.3f)
    {
        if (textComponent == null) return null;
        Transform tr = textComponent.transform;
        string tweenId = tr.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        textComponent.text = content;
        textComponent.color = textColor;
        tr.localScale = Vector3.one * startScale;

        Vector3 startPos = tr.position;
        Vector3 targetPos = startPos + (direction.normalized * distance);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        // 1. 스케일 축소 및 위치 상승 (동시 진행)
        seq.Append(tr.DOScale(Vector3.one, duration * 0.25f).SetEase(Ease.OutBack));
        seq.Join(tr.DOMove(targetPos, duration).SetEase(Ease.OutCubic));
        // 2. 후반 페이드 아웃
        seq.Insert(duration * 0.5f, textComponent.DOFade(0f, duration * 0.5f).SetEase(Ease.InQuad));
        return seq;
    }

    /// <summary>
    /// 빨간색 플로팅 텍스트 (FloatingText와 동일)
    /// </summary>
    public static Sequence DamageNumber(TMP_Text textComp, int damage, float duration = 0.6f)
        => FloatingText(textComp, $"-{damage}", new Color(1f, 0.25f, 0.2f), Vector3.up, 0.4f, duration, 1.4f);

    /// <summary>
    /// 초록색 플로팅 텍스트 (FloatingText와 동일)
    /// </summary>
    public static Sequence HealNumber(TMP_Text textComp, int heal, float duration = 0.6f)
        => FloatingText(textComp, $"+{heal}", new Color(0.2f, 1f, 0.35f), Vector3.up, 0.35f, duration, 1.2f);

    /// <summary>
    /// 노란색 플로팅 텍스트 (FloatingText와 동일)
    /// </summary>
    public static Sequence BuffNumber(TMP_Text textComp, string buffName, float duration = 0.7f)
        => FloatingText(textComp, buffName, new Color(1f, 0.9f, 0f), Vector3.up, 0.3f, duration, 1.1f);

    /// <summary>
    /// 보라색 플로팅 텍스트 (FloatingText와 동일)
    /// </summary>
    public static Sequence DebuffNumber(TMP_Text textComp, string debuffName, float duration = 0.7f)
        => FloatingText(textComp, debuffName, new Color(0.85f, 0.4f, 1f), Vector3.down, 0.3f, duration, 1.1f);

    /// <summary>
    /// 기본 피격 연출: 대상의 스케일 펀치 + 흔들림 + 백색 플래시.
    /// </summary>
    /// <param name="target">피격 대상 GameObject</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence Hit(GameObject target, float duration = 0.2f)
    {
        if (target == null) return null;
        Sequence seq = DOTween.Sequence();
        seq.Join(PunchScale(target.transform, 0.12f, duration, 6));
        seq.Join(ShakePosition(target.transform, duration, 0.08f, 12));
        seq.Join(Flash(target, new Color(1f, 0.9f, 0.9f, 1f), duration, 1));
        return seq;
    }

    /// <summary>
    /// 강력한 피격 연출: 강한 흔들림 및 붉은 플래시 + 선택적 카메라 흔들림.
    /// </summary>
    /// <param name="target">피격 대상 GameObject</param>
    /// <param name="camTransform">흔들릴 메인 카메라 Transform (선택 사항)</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence StrongHit(GameObject target, Transform camTransform = null, float duration = 0.3f)
    {
        if (target == null) return null;
        Sequence seq = DOTween.Sequence();
        seq.Join(PunchScale(target.transform, 0.25f, duration, 8));
        seq.Join(ShakePosition(target.transform, duration, 0.18f, 15));
        seq.Join(Flash(target, new Color(1f, 0.2f, 0.2f, 1f), duration, 2));

        if (camTransform != null)
        {
            seq.Join(CameraShake(camTransform, duration, 0.15f, 15));
        }
        return seq;
    }

    #endregion

    #region 6. HP / Slider / Value 연출

    /// <summary>
    /// Image.fillAmount를 부드럽게 감쇄/증가시킵니다.
    /// </summary>
    /// <param name="hpBarImage">대상 UI Image (Filled 타입)</param>
    /// <param name="targetFill">목표 Fill 수치 (0.0 ~ 1.0)</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween HPBarTo(Image hpBarImage, float targetFill, float duration = 0.3f, Ease ease = Ease.OutQuad)
    {
        if (hpBarImage == null) return null;
        string tweenId = hpBarImage.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return hpBarImage.DOFillAmount(targetFill, duration).SetEase(ease).SetId(tweenId);
    }

    /// <summary>
    /// Slider.value를 부드럽게 변경합니다.
    /// </summary>
    /// <param name="hpSlider">대상 UI Slider</param>
    /// <param name="targetValue">목표 Slider Value 수치</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween HPBarTo(Slider hpSlider, float targetValue, float duration = 0.3f, Ease ease = Ease.OutQuad)
    {
        if (hpSlider == null) return null;
        string tweenId = hpSlider.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return hpSlider.DOValue(targetValue, duration).SetEase(ease).SetId(tweenId);
    }

    /// <summary>
    /// 정수 텍스트가 목표치까지 1씩 차감/증가하며 정밀하게 카운팅되도록 연출합니다.
    /// </summary>
    /// <param name="textComp">표시할 TextMeshPro 컴포넌트</param>
    /// <param name="fromValue">시작 정수 값</param>
    /// <param name="toValue">목표 정수 값</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Tween HPTextTo(TMP_Text textComp, int fromValue, int toValue, float duration = 0.3f)
    {
        if (textComp == null) return null;
        string tweenId = textComp.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        int currentValue = fromValue;
        return DOTween.To(() => currentValue, x =>
        {
            currentValue = x;
            if (textComp != null) textComp.text = currentValue.ToString();
        }, toValue, duration).SetEase(Ease.OutQuad).SetId(tweenId);
    }

    /// <summary>
    /// 체력 바 게이지와 체력 텍스트를 동시에 애니메이션으로 깎아내는 복합 피드백입니다.
    /// </summary>
    /// <param name="hpBar">체력 게이지 UI Image</param>
    /// <param name="hpText">체력 숫자 UI Text</param>
    /// <param name="targetFill">목표 Fill 비율 (0.0 ~ 1.0)</param>
    /// <param name="fromHp">시작 HP 수치</param>
    /// <param name="toHp">목표 HP 수치</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence HPDamage(Image hpBar, TMP_Text hpText, float targetFill, int fromHp, int toHp, float duration = 0.35f)
    {
        Sequence seq = DOTween.Sequence();
        if (hpBar != null) seq.Join(HPBarTo(hpBar, targetFill, duration));
        if (hpText != null) seq.Join(HPTextTo(hpText, fromHp, toHp, duration));
        return seq;
    }

    /// <summary>
    /// 체력 바 게이지와 체력 텍스트를 동시에 회복시키는 복합 피드백입니다.
    /// </summary>
    /// <param name="hpBar">체력 게이지 UI Image</param>
    /// <param name="hpText">체력 숫자 UI Text</param>
    /// <param name="targetFill">목표 Fill 비율 (0.0 ~ 1.0)</param>
    /// <param name="fromHp">시작 HP 수치</param>
    /// <param name="toHp">목표 HP 수치</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence HPHeal(Image hpBar, TMP_Text hpText, float targetFill, int fromHp, int toHp, float duration = 0.35f)
    {
        return HPDamage(hpBar, hpText, targetFill, fromHp, toHp, duration);
    }

    #endregion

    #region 7. UI 기본 효과 (Buttons, Panels, Scales)

    /// <summary>
    /// 버튼에 마우스를 올렸을 때 확대되는 호버 연출입니다.
    /// </summary>
    /// <param name="buttonTr">버튼 Transform</param>
    /// <param name="scaleMultiplier">확대 배율</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Tween ButtonHover(Transform buttonTr, float scaleMultiplier = 1.05f, float duration = 0.1f)
        => ScaleMultiplier(buttonTr, scaleMultiplier, duration, Ease.OutQuad);

    /// <summary>
    /// 버튼에서 마우스가 이탈할 때 원본 크기로 복귀하는 연출입니다.
    /// </summary>
    /// <param name="buttonTr">버튼 Transform</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Tween ButtonHoverExit(Transform buttonTr, Vector3 originScale, float duration = 0.1f)
    {
        if (buttonTr == null) return null;
        return Scale(buttonTr, originScale, duration, Ease.OutQuad);
    }

    /// <summary>
    /// 버튼 클릭 시 살짝 눌렸다가 튕겨 나오는 피드백 연출입니다.
    /// </summary>
    /// <param name="buttonTr">버튼 Transform</param>
    /// <param name="pressDownMult">눌렸을 때의 축소 배율</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="onClickAction">클릭 순간(수축 직후) 실행될 콜백</param>
    public static Sequence ButtonPress(Transform buttonTr, Vector3 originScale, float pressDownMult = 0.94f, float duration = 0.12f, Action onClickAction = null)
    {
        if (buttonTr == null) return null;
        string tweenId = buttonTr.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        seq.Append(buttonTr.DOScale(originScale * pressDownMult, duration * 0.4f).SetEase(Ease.OutQuad));
        seq.Append(buttonTr.DOScale(originScale, duration * 0.6f).SetEase(Ease.OutBack));
        seq.AppendCallback(() => onClickAction?.Invoke());
        return seq;
    }

    /// <summary>
    /// UI 요소를 일시적으로 통통 튀게 만드는 팝 효과입니다.
    /// </summary>
    /// <param name="uiTransform">대상 UI Transform</param>
    /// <param name="punchScale">팝업 강도</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Tween UIPop(Transform uiTransform, float punchScale = 0.1f, float duration = 0.15f)
        => PunchScale(uiTransform, punchScale, duration, 5);

    /// <summary>
    /// 팝업창, 메뉴창이 열릴 때 알파 페이드인과 함께 살짝 커지며 등장합니다.
    /// </summary>
    /// <param name="panelObj">열 패널 GameObject</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="onComplete">열기 연출 완료 시 콜백</param>
    public static Sequence PanelOpen(GameObject panelObj, float duration = 0.2f, Action onComplete = null)
    {
        if (panelObj == null) return null;
        panelObj.SetActive(true);

        Transform tr = panelObj.transform;
        tr.localScale = Vector3.one * 0.92f;
        Fade(panelObj, 0f, 0f); // 즉시 0으로 초기화

        Sequence seq = DOTween.Sequence();
        seq.Join(FadeIn(panelObj, duration, Ease.OutQuad));
        seq.Join(tr.DOScale(Vector3.one, duration).SetEase(Ease.OutBack));
        seq.OnComplete(() => onComplete?.Invoke());
        return seq;
    }

    /// <summary>
    /// 팝업창, 메뉴창이 닫힐 때 알파 페이드아웃과 함께 축소된 후 비활성화합니다.
    /// </summary>
    /// <param name="panelObj">닫을 패널 GameObject</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="onComplete">비활성화 후 실행할 콜백</param>
    public static Sequence PanelClose(GameObject panelObj, float duration = 0.15f, Action onComplete = null)
    {
        if (panelObj == null) return null;

        Sequence seq = DOTween.Sequence();
        seq.Join(FadeOut(panelObj, duration, Ease.InQuad));
        seq.Join(panelObj.transform.DOScale(Vector3.one * 0.92f, duration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            panelObj.SetActive(false);
            onComplete?.Invoke();
        });
        return seq;
    }

    /// <summary>
    /// RectTransform의 AnchoredPosition을 사용하여 UI를 부드럽게 화면 밖에서 안으로 슬라이드합니다.
    /// </summary>
    /// <param name="rectTr">대상 UI RectTransform</param>
    /// <param name="fromAnchoredPos">시작 앵커 위치</param>
    /// <param name="toAnchoredPos">목표 앵커 위치</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween UISlideIn(RectTransform rectTr, Vector2 fromAnchoredPos, Vector2 toAnchoredPos, float duration = 0.25f, Ease ease = Ease.OutQuad)
    {
        if (rectTr == null) return null;
        rectTr.anchoredPosition = fromAnchoredPos;
        return rectTr.DOAnchorPos(toAnchoredPos, duration).SetEase(ease);
    }

    /// <summary>
    /// UI를 화면 밖 지정된 위치로 슬라이드하여 퇴장시킵니다.
    /// </summary>
    /// <param name="rectTr">대상 UI RectTransform</param>
    /// <param name="toAnchoredPos">퇴장할 목표 앵커 위치</param>
    /// <param name="duration">진행 시간 (초)</param>
    /// <param name="ease">이징 곡선</param>
    public static Tween UISlideOut(RectTransform rectTr, Vector2 toAnchoredPos, float duration = 0.2f, Ease ease = Ease.InQuad)
    {
        if (rectTr == null) return null;
        return rectTr.DOAnchorPos(toAnchoredPos, duration).SetEase(ease);
    }

    #endregion

    #region 8. Turn / Battle System 연출

    /// <summary>
    /// 턴 전환 텍스트 안내 ("TURN 1", "ENEMY TURN") 연출:
    /// 등장(Scale Up + Fade In) -> 잠시 멈춤 -> 퇴장(Fade Out).
    /// </summary>
    /// <param name="bannerObj">턴 배너 UI GameObject</param>
    /// <param name="stayDuration">중앙에서 대기하는 시간 (초)</param>
    /// <param name="transitionDuration">등장/퇴장 애니메이션 시간 (초)</param>
    /// <param name="onComplete">배너 퇴장 완료 시 실행할 콜백</param>
    public static Sequence TurnStart(GameObject bannerObj, float stayDuration = 0.5f, float transitionDuration = 0.2f, Action onComplete = null)
    {
        if (bannerObj == null) return null;
        bannerObj.SetActive(true);

        Transform tr = bannerObj.transform;
        tr.localScale = Vector3.one * 0.8f;
        Fade(bannerObj, 0f, 0f);

        Sequence seq = DOTween.Sequence();
        // 1. 진입
        seq.Append(tr.DOScale(Vector3.one, transitionDuration).SetEase(Ease.OutBack));
        seq.Join(FadeIn(bannerObj, transitionDuration));
        // 2. 유지
        seq.AppendInterval(stayDuration);
        // 3. 퇴장
        seq.Append(FadeOut(bannerObj, transitionDuration));
        seq.Join(tr.DOScale(Vector3.one * 1.1f, transitionDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            bannerObj.SetActive(false);
            onComplete?.Invoke();
        });
        return seq;
    }

    /// <summary>
    /// 카드 순차 실행 단계에서 현재 행동 중인 카드를 하이라이트하고,
    /// 나머지 카드들을 흐리게(Dim) 만들어 시선의 분산을 막습니다.
    /// </summary>
    /// <param name="currentCard">하이라이트할 현재 실행 카드 Transform</param>
    /// <param name="otherCards">Dim(어둡게/흐리게) 처리할 나머지 카드 리스트</param>
    /// <param name="highlightScale">현재 카드의 확대 배율</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence CurrentExecutionHighlight(Transform currentCard, Vector3 currentCardOriginScale, List<Transform> otherCards, Dictionary<Transform, Vector3> otherOriginScales = null, float highlightScale = 1.15f, float duration = 0.15f)
    {
        Sequence seq = DOTween.Sequence();

        // 1. 현재 카드 부각 (원본 스케일 기반)
        if (currentCard != null)
        {
            seq.Join(Scale(currentCard, currentCardOriginScale * highlightScale, duration, Ease.OutBack));
            seq.Join(Flash(currentCard.gameObject, Color.yellow, duration, 1));
        }

        // 2. 나머지 카드 감쇄 (Dim)
        if (otherCards != null)
        {
            foreach (var other in otherCards)
            {
                if (other == null || other == currentCard) continue;

                Vector3 targetDimScale = (otherOriginScales != null && otherOriginScales.TryGetValue(other, out var origin))
                    ? origin * 0.95f
                    : other.localScale * 0.95f;

                seq.Join(Fade(other.gameObject, 0.45f, duration));
                seq.Join(Scale(other, targetDimScale, duration));
            }
        }

        return seq;
    }

    /// <summary>
    /// 실행 완료 후 카드들의 흐림(Dim) 상태 및 스케일을 원래대로 복원합니다.
    /// </summary>
    /// <param name="allCards">복원할 모든 카드 Transform 리스트</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence ResetExecutionHighlight(List<Transform> allCards, Dictionary<Transform, Vector3> originScales = null, float duration = 0.15f)
    {
        Sequence seq = DOTween.Sequence();
        if (allCards == null) return seq;

        foreach (var card in allCards)
        {
            if (card == null) continue;

            Vector3 targetScale = (originScales != null && originScales.TryGetValue(card, out var origin))
                ? origin
                : Vector3.one;

            seq.Join(Fade(card.gameObject, 1f, duration));
            seq.Join(Scale(card, targetScale, duration));
        }
        return seq;
    }

    #endregion

    #region 9. Battle Result (승리 / 패배 연출)

    /// <summary>
    /// 전투 승리 패널 연출: 배경 암전 -> 승리 텍스트 스케일 펀치.
    /// </summary>
    /// <param name="victoryPanel">승리 전체 패널 GameObject</param>
    /// <param name="bannerTr">승리 텍스트/배너 이미지 Transform</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence Victory(GameObject victoryPanel, Transform bannerTr, float duration = 0.4f)
    {
        if (victoryPanel == null) return null;
        victoryPanel.SetActive(true);

        Sequence seq = DOTween.Sequence();
        seq.Append(FadeIn(victoryPanel, duration * 0.5f));

        if (bannerTr != null)
        {
            bannerTr.localScale = Vector3.one * 0.5f;
            seq.Append(bannerTr.DOScale(Vector3.one, duration).SetEase(Ease.OutBounce));
            seq.Append(bannerTr.DOPunchScale(Vector3.one * 0.1f, 0.3f, 4));
        }
        return seq;
    }

    /// <summary>
    /// 전투 패배 패널 연출: 붉은 톤 페이드인 -> 둔탁한 하향 안착.
    /// </summary>
    /// <param name="defeatPanel">패배 전체 패널 GameObject</param>
    /// <param name="bannerTr">패배 텍스트/배너 이미지 Transform</param>
    /// <param name="duration">진행 시간 (초)</param>
    public static Sequence Defeat(GameObject defeatPanel, Transform bannerTr, float duration = 0.4f)
    {
        if (defeatPanel == null) return null;
        defeatPanel.SetActive(true);

        Sequence seq = DOTween.Sequence();
        seq.Append(FadeIn(defeatPanel, duration * 0.5f));

        if (bannerTr != null)
        {
            Vector3 originPos = bannerTr.position;
            bannerTr.position = originPos + (Vector3.up * 0.5f);
            seq.Append(bannerTr.DOMove(originPos, duration).SetEase(Ease.OutCubic));
            seq.Join(bannerTr.DOShakePosition(0.2f, 0.08f, 10));
        }
        return seq;
    }

    #endregion

    #region 10. Camera 효과

    /// <summary>
    /// 카드게임용 카메라 진동 효과입니다. 지나친 어지러움을 막기 위해 정밀하게 제한된 진폭을 사용합니다.
    /// </summary>
    /// <param name="camTransform">카메라 Transform</param>
    /// <param name="duration">지속 시간 (0.1 ~ 0.25초 권장)</param>
    /// <param name="strength">진동 강도 (약한 타격: 0.05f, 강한 타격: 0.15f)</param>
    /// <param name="vibrato">초당 진동 횟수</param>
    public static Tween CameraShake(Transform camTransform, float duration = 0.15f, float strength = 0.08f, int vibrato = 12)
    {
        if (camTransform == null) return null;
        string tweenId = camTransform.GetInstanceID() + ID_TRANSFORM;
        DOTween.Kill(tweenId);

        return camTransform.DOShakePosition(duration, strength, vibrato, 90f, false, true).SetId(tweenId);
    }

    #endregion
}