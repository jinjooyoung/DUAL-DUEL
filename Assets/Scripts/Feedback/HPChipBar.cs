using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체력 바 잔상. 실제 체력 바(front)는 기존처럼 즉시 줄이고, 그 뒤에 깔린 밝은 잔상 바(chip)가
/// delay 후 천천히 따라 줄어들어 "깎여 나가는" 느낌을 준다. 회복은 잔상 없이 즉시 맞춤.
/// 설정: 기존 체력 바 Image 를 복제해 바로 뒤(형제 순서 앞)에 두고 흰색으로 -> chip 에 연결.
/// </summary>
public class HPChipBar : MonoBehaviour
{
    public enum Mode
    {
        [Tooltip("Image Type = Filled 인 바 (fillAmount)")] ImageFill,
        [Tooltip("RectTransform 의 localScale.x 로 줄이는 바")] ScaleX,
    }

    public Mode mode = Mode.ImageFill;
    [Tooltip("실제 체력 바")] public Image front;
    [Tooltip("뒤에 깔린 잔상 바 (흰색 권장)")] public Image chip;
    [Tooltip("피해 후 잔상이 머무는 시간")] public float delay = 0.35f;
    [Tooltip("잔상이 줄어드는 속도 (초당 바 전체 대비 비율)")] public float speed = 1.2f;
    [Tooltip("피해 직후 잔상을 이 시간 동안 깜빡임")] public float blinkTime = 0.15f;

    float _hold, _blink, _last = -1f;

    void LateUpdate()
    {
        if (front == null || chip == null) return;
        float f = Get(front), c = Get(chip), dt = Time.unscaledDeltaTime;

        if (_last >= 0f && f < _last - 1e-4f) { _hold = delay; _blink = blinkTime; }   // 새로 맞음
        _last = f;

        if (_blink > 0f)
        {
            _blink -= dt;
            chip.enabled = (Time.frameCount / 2) % 2 == 0 || _blink <= 0f;
        }
        else chip.enabled = true;

        if (f >= c) { Set(chip, f); return; }   // 회복: 즉시
        if (_hold > 0f) { _hold -= dt; return; }
        Set(chip, Mathf.MoveTowards(c, f, speed * dt));
    }

    float Get(Image img) => mode == Mode.ImageFill ? img.fillAmount : img.rectTransform.localScale.x;

    void Set(Image img, float v)
    {
        if (mode == Mode.ImageFill) img.fillAmount = v;
        else { Vector3 s = img.rectTransform.localScale; s.x = v; img.rectTransform.localScale = s; }
    }
}
