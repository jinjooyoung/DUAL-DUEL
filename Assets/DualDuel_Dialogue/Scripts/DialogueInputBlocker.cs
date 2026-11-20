using UnityEngine;

/// <summary>
/// 대사창이 떠 있는 동안 카메라 바로 앞에 화면 전체를 덮는 투명 콜라이더를 둬서,
/// OnMouseEnter / OnMouseDown 을 쓰는 카드, 스테이지 노드, 맵 드래그를 한 번에 막는다.
///   - 3D(BoxCollider) 와 2D(BoxCollider2D) 를 둘 다 둠: 유니티는 마우스 이벤트용 레이캐스트를 3D 와 2D 로 따로 해서
///     하나만 두면 다른 쪽(예: BoxCollider2D 카드)은 막히지 않음. 한 오브젝트에 같이 붙일 수 없어 자식 2개로 나눔
///   - 켜져 있는 동안 매 프레임 메인 카메라를 따라감 (스크롤, 씬 이동으로 카메라가 바뀌어도)
///   - 캔버스 아래에 두면 캔버스가 크기/위치를 바꾸므로 씬이 바뀌어도 유지되는 별도 오브젝트로 만듦
/// DialogueManager 가 Create 로 만들고 SetBlocking 으로 켜고 끈다.
/// </summary>
public class DialogueInputBlocker : MonoBehaviour
{
    [Tooltip("카메라 Near Clip 에서 이만큼 앞에 둠 (모든 카드/노드보다 카메라에 가까워야 함)")]
    [SerializeField] private float distanceFromNearClip = 0.2f;
    [Tooltip("화면보다 이 배율만큼 크게 덮음 (카메라 흔들림, 화면비 변화 대비)")]
    [SerializeField] private float sizeMargin = 1.5f;

    private GameObject block3D;
    private GameObject block2D;
    private Camera cam;
    private bool blocking;

    /// <summary>
    /// 지금 입력을 막고 있는지
    /// </summary>
    public bool IsBlocking => blocking;

    /// <summary>
    /// 블로커 생성 (씬이 바뀌어도 유지). DialogueManager 가 처음 한 번 호출
    /// </summary>
    public static DialogueInputBlocker Create()
    {
        var go = new GameObject("DialogueInputBlocker");
        DontDestroyOnLoad(go);
        return go.AddComponent<DialogueInputBlocker>();
    }

    private void Awake()
    {
        // 3D 와 2D 콜라이더는 한 오브젝트에 같이 붙일 수 없어서 자식 2개로
        block3D = new GameObject("Block3D", typeof(BoxCollider));
        block3D.transform.SetParent(transform, false);
        block3D.GetComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.01f);

        block2D = new GameObject("Block2D", typeof(BoxCollider2D));
        block2D.transform.SetParent(transform, false);
        block2D.GetComponent<BoxCollider2D>().size = Vector2.one;

        SetBlocking(false);
    }

    /// <summary>
    /// 입력 막기 켜기/끄기 (대사창을 열 때 true, 닫을 때 false)
    /// </summary>
    public void SetBlocking(bool on)
    {
        blocking = on;
        if (on)
        {
            cam = Camera.main;
            if (cam != null)
            {
                SetLayer(PickLayer(cam));
                Follow();
            }
        }
        Activate(on && cam != null);
    }

    // 켜져 있는 동안 카메라를 따라감 (카메라 이동이 끝난 뒤에 맞추려고 LateUpdate)
    private void LateUpdate()
    {
        if (!blocking) return;

        // 씬이 바뀌어 카메라가 사라졌으면 새 메인 카메라를 찾음
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) { Activate(false); return; }
            SetLayer(PickLayer(cam));
            Activate(true);
        }
        Follow();
    }

    // 카메라 바로 앞, 화면을 다 덮는 크기로
    private void Follow()
    {
        Transform ct = cam.transform;
        float distance = cam.nearClipPlane + distanceFromNearClip;

        float height = cam.orthographic
            ? cam.orthographicSize * 2f
            : 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * cam.aspect;

        transform.SetPositionAndRotation(ct.position + ct.forward * distance, ct.rotation);
        transform.localScale = new Vector3(width * sizeMargin, height * sizeMargin, 1f);
    }

    // 카메라가 마우스 이벤트를 보내는 레이어 중 하나 (Default 우선, Ignore Raycast 제외)
    private static int PickLayer(Camera camera)
    {
        int mask = camera.cullingMask & camera.eventMask;
        int ignore = LayerMask.NameToLayer("Ignore Raycast");
        if ((mask & 1) != 0) return 0;   // Default
        for (int i = 0; i < 32; i++)
            if (i != ignore && (mask & (1 << i)) != 0) return i;
        return 0;
    }

    // 자식 콜라이더 레이어 맞추기
    private void SetLayer(int layer)
    {
        gameObject.layer = layer;
        block3D.layer = layer;
        block2D.layer = layer;
    }

    // 콜라이더 켜기/끄기
    private void Activate(bool on)
    {
        if (block3D != null && block3D.activeSelf != on) block3D.SetActive(on);
        if (block2D != null && block2D.activeSelf != on) block2D.SetActive(on);
    }
}
