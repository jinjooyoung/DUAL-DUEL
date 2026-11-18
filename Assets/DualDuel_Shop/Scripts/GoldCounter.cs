using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>상점 우상단 보유 골드. 바뀌면 숫자가 굴러가듯 줄어들거나 늘어남</summary>
public class GoldCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float rollTime = 0.6f;

    private int shown;
    private Tween rolling;

    private void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();
    }

    private void OnDestroy() => rolling?.Kill();

    public void SetInstant(int value)
    {
        rolling?.Kill();
        shown = value;
        if (text != null) text.text = value.ToString();
    }

    /// <summary>현재 표시값에서 value 까지 rollTime 동안 굴러감 (빠르게 시작해 천천히 멈춤)</summary>
    public void RollTo(int value)
    {
        rolling?.Kill();
        rolling = DOTween.To(() => shown, x => { shown = x; if (text != null) text.text = x.ToString(); }, value, rollTime)
            .SetEase(Ease.OutCubic);
    }
}
