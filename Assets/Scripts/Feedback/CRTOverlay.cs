using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전체를 덮는 CRT 스캔라인. 텍스처는 실행 시 코드로 생성 (별도 에셋 불필요)
/// 입력을 막지 않도록 RawImage의 Raycast Target은 꺼짐
/// </summary>
[RequireComponent(typeof(RawImage))]
public class CRTOverlay : MonoBehaviour
{
    [Tooltip("스캔라인 한 줄의 높이 (화면 픽셀)")]
    [SerializeField, Range(1, 8)] private int linePixelHeight = 2;
    [Tooltip("어두운 줄의 진하기")]
    [SerializeField, Range(0f, 1f)] private float darkness = 0.3f;
    [Tooltip("밝기 깜빡임 정도")]
    [SerializeField, Range(0f, 0.2f)] private float flickerAmount = 0.04f;
    [SerializeField] private float flickerSpeed = 9f;
    [Tooltip("스캔라인이 위로 흐르는 속도 (0이면 고정)")]
    [SerializeField] private float rollSpeed = 0.15f;

    private RawImage image;
    private Texture2D lineTexture;
    private float offset;

    private void Awake()
    {
        image = GetComponent<RawImage>();
        image.raycastTarget = false;

        // 1 x 2 텍스처: 위 = 어두운 줄, 아래 = 투명
        lineTexture = new Texture2D(1, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            name = "CRT_Scanline"
        };
        lineTexture.SetPixels(new[] { new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 1f) });
        lineTexture.Apply();

        image.texture = lineTexture;
    }

    private void Update()
    {
        // 텍스처 1장 = 줄 2개(어두운 줄 + 투명 줄) -> 화면 높이에 맞춰 반복 횟수 계산
        float repeat = Screen.height / (linePixelHeight * 2f);
        offset = (offset + rollSpeed * Time.unscaledDeltaTime) % 1f;
        image.uvRect = new Rect(0f, offset, 1f, repeat);

        float flicker = (Mathf.PerlinNoise(0f, Time.unscaledTime * flickerSpeed) - 0.5f) * 2f * flickerAmount;
        image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(darkness + flicker));
    }

    private void OnDestroy()
    {
        if (lineTexture != null) Destroy(lineTexture);
    }
}
