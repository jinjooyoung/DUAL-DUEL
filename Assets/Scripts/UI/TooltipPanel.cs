using UnityEngine;
using TMPro;

public class TooltipPanel : MonoBehaviour
{
    public static TooltipPanel Instance { get; private set; }

    [Header("UI 연결")]
    [SerializeField] private GameObject tooltipObj;          // 툴팁 루트 (TooltipBG)
    [SerializeField] private TextMeshProUGUI tooltipTextArea; // 텍스트 컴포넌트
    [SerializeField] private Canvas targetCanvas;            // 부모 Canvas

    [Header("패딩 및 크기 제한")]
    [SerializeField] private Vector2 padding = new Vector2(30f, 20f); // 좌우 여백 합(30), 상하 여백 합(20)
    [SerializeField] private float maxTextWidth = 320f;              // 최대 가로폭 (이 폭을 넘기면 줄바꿈)

    private RectTransform canvasRectTransform;
    private RectTransform tooltipRectTransform;
    private RectTransform textRectTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();
        canvasRectTransform = targetCanvas.GetComponent<RectTransform>();
        tooltipRectTransform = tooltipObj.GetComponent<RectTransform>();
        textRectTransform = tooltipTextArea.GetComponent<RectTransform>();

        // 시작할 땐 비활성화
        tooltipObj.SetActive(false);
    }

    private void LateUpdate()
    {
        // 툴팁이 켜져 있을 때만 마우스 위치 계산
        if (tooltipObj.activeInHierarchy)
        {
            CalcMousePosition();
        }
    }

    /// <summary>
    /// 텍스트 길이에 맞춰 TooltipBG의 크기를 실시간 계산하여 띄웁니다.
    /// </summary>
    public void ShowTooltip(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        tooltipTextArea.SetText(text);

        // 1. TextMeshPro가 차지할 실제 가로/세로 픽셀 크기 계산
        Vector2 textSize = tooltipTextArea.GetPreferredValues(text, maxTextWidth, 0f);

        // 2. 텍스트 오브젝트 본체의 크기 조절
        textRectTransform.sizeDelta = textSize;

        // 3. 배경(TooltipBG) 크기 = 텍스트 크기 + 여백(Padding)
        tooltipRectTransform.sizeDelta = new Vector2(textSize.x + padding.x, textSize.y + padding.y);

        // 4. 활성화 및 마우스 위치 갱신
        tooltipObj.SetActive(true);
        CalcMousePosition();
    }

    public void HideTooltip()
    {
        if (tooltipObj != null)
        {
            tooltipObj.SetActive(false);
        }
    }

    /// <summary>
    /// 마우스 위치 변환 및 화면 끝(75%) 반전 로직
    /// </summary>
    private void CalcMousePosition()
    {
        Vector2 mousePos = Input.mousePosition;

        Camera cam = (targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? targetCanvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            mousePos,
            cam,
            out Vector2 localPos
        );

        // 화면 가로 75%를 넘어가면 마우스 왼쪽에 나타나도록 오프셋 반전
        if (mousePos.x > Screen.width * 0.75f)
        {
            localPos.x -= tooltipRectTransform.sizeDelta.x + 15f;
        }
        else
        {
            localPos.x += 15f; // 마우스 커서 우측 살짝 띄우기
        }

        // 마우스 커서 살짝 아래로 내려서 표시
        localPos.y -= 10f;

        tooltipRectTransform.anchoredPosition = localPos;
    }
}