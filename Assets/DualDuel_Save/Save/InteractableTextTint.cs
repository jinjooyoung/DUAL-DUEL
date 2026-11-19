using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼(Selectable)의 Interactable 이 꺼지면 글자를 어둡게, 켜지면 원래 색으로 되돌림.
/// 버튼과 같은 오브젝트에 붙이면 같은 오브젝트의 Button 과 자식의 모든 TMP 글자를 자동으로 사용.
/// (유니티 버튼에는 Interactable 변경 이벤트가 없어서, 상태가 바뀐 순간에만 색을 바꿈)
/// </summary>
public class InteractableTextTint : MonoBehaviour
{
    [Tooltip("비우면 같은 오브젝트의 Button(Selectable) 사용")]
    [SerializeField] private Selectable target;
    [Tooltip("비우면 자식의 모든 TMP 글자 사용")]
    [SerializeField] private List<TMP_Text> texts = new List<TMP_Text>();
    [Tooltip("누를 수 없을 때의 글자 색")]
    [SerializeField] private Color disabledColor = new Color(0.33f, 0.33f, 0.33f, 1f);

    private readonly List<Color> originalColors = new List<Color>();
    private bool lastState;
    private bool initialized;

    private void Awake()
    {
        if (target == null) target = GetComponent<Selectable>();
        if (texts.Count == 0) texts.AddRange(GetComponentsInChildren<TMP_Text>(true));
        foreach (TMP_Text t in texts) originalColors.Add(t != null ? t.color : Color.white);
    }

    private void OnEnable()
    {
        initialized = false;   // 다시 켜지면 현재 상태로 한 번 맞춤
        Apply();
    }

    // Interactable 이 바뀐 순간에만 색 적용
    private void LateUpdate()
    {
        if (target == null) return;
        bool state = target.IsInteractable();
        if (initialized && state == lastState) return;
        Apply();
    }

    /// <summary>
    /// 원래 색을 다시 기억 (글자 색을 코드로 바꾼 뒤 그 색을 기준으로 삼고 싶을 때)
    /// </summary>
    public void RecaptureColors()
    {
        for (int i = 0; i < texts.Count; i++)
            if (texts[i] != null) originalColors[i] = texts[i].color;
        initialized = false;
        Apply();
    }

    // 현재 상태에 맞는 색으로
    private void Apply()
    {
        if (target == null) return;
        lastState = target.IsInteractable();
        initialized = true;
        for (int i = 0; i < texts.Count; i++)
            if (texts[i] != null) texts[i].color = lastState ? originalColors[i] : disabledColor;
    }
}
