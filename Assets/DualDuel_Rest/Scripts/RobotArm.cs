using UnityEngine;

/// <summary>
/// UI 위의 2관절 기계팔. target(같은 부모 기준 anchoredPosition)을 주면 팔꿈치 각도를 계산해 용접기 끝을 거기 맞춘다.
/// upper / fore / welder 는 같은 부모 아래 형제이고, 각자 피벗이 위쪽 관절에 있다.
/// </summary>
public class RobotArm : MonoBehaviour
{
    public RectTransform shoulder;   // 고정점 (arm_base)
    public RectTransform upper;
    public RectTransform fore;
    public RectTransform welder;
    public float upperLength = 300f;   // 관절 사이 거리 (캔버스 단위)
    public float foreLength = 260f;
    public float tipLength = 52f;      // 용접기 관절에서 불꽃 끝까지
    [Tooltip("팔꿈치가 꺾이는 방향 (+1 / -1)")] public float bend = 1f;

    public Vector2 target;
    public Vector2 Tip { get; private set; }

    void LateUpdate() => Solve();

    public void Solve()
    {
        if (shoulder == null || upper == null || fore == null) return;
        Vector2 s = shoulder.anchoredPosition;
        Vector2 d = target - s;
        float l1 = upperLength, l2 = foreLength + tipLength;
        float dist = Mathf.Clamp(d.magnitude, Mathf.Abs(l1 - l2) + 1f, l1 + l2 - 0.01f);
        float baseAng = Mathf.Atan2(d.y, d.x);
        float cosA = Mathf.Clamp((l1 * l1 + dist * dist - l2 * l2) / (2f * l1 * dist), -1f, 1f);
        float a1 = baseAng + bend * Mathf.Acos(cosA);
        Vector2 elbow = s + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * l1;
        Vector2 reach = s + (d.sqrMagnitude > 1e-4f ? d.normalized : Vector2.down) * dist;
        Vector2 dir2 = (reach - elbow).normalized;
        float a2 = Mathf.Atan2(dir2.y, dir2.x);

        // 피벗에서 아래(-Y)로 뻗은 이미지를 a 방향으로 돌리려면 z = a + 90°
        upper.anchoredPosition = s;
        upper.localEulerAngles = new Vector3(0f, 0f, a1 * Mathf.Rad2Deg + 90f);
        fore.anchoredPosition = elbow;
        fore.localEulerAngles = new Vector3(0f, 0f, a2 * Mathf.Rad2Deg + 90f);
        Vector2 wrist = elbow + dir2 * foreLength;
        if (welder != null)
        {
            welder.anchoredPosition = wrist;
            welder.localEulerAngles = fore.localEulerAngles;
        }
        Tip = wrist + dir2 * tipLength;
    }
}
