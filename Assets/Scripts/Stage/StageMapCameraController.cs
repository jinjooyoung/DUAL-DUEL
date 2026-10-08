using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class StageMapCameraController : MonoBehaviour
{
    [Header("타겟 카메라")]
    [SerializeField] private Camera targetCamera;

    [Header("이동 감도 설정")]
    [SerializeField] private float dragSpeed = 1.0f;     // 마우스 드래그 감도
    [SerializeField] private float scrollSpeed = 5.0f;   // 마우스 휠 감도
    [SerializeField] private float smoothTime = 0.1f;    // 감속 보간 시간

    [Header("좌표 설정")]
    [SerializeField] private float minX = 10f;           // 최소 시작 X좌표
    [SerializeField] private float fixedY = -1f;         // 고정 Y좌표 (-1f)
    private float maxX = 10f;                            // 마지막 노드 기준 최대 X좌표

    private Vector3 dragOriginWorld;
    private float targetCameraX;
    private float currentVelocityX;
    private bool isDragging = false;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // 1. 카메라 및 배경의 초기 위치를 (10, -1)로 설정
        targetCameraX = minX;
        Vector3 camPos = targetCamera.transform.position;
        camPos.x = minX;
        camPos.y = fixedY;
        targetCamera.transform.position = camPos;

        UpdateBackgroundPosition();

        // 2. 최대 이동 가능 X좌표 계산
        CalculateMaxX();
    }

    private void Update()
    {
        // UI 요소를 클릭 중일 때는 맵 드래그/스크롤 차단
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 3. 마우스 휠 입력 처리 (휠 아래: 오른쪽, 휠 위: 왼쪽)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.001f)
        {
            targetCameraX -= scrollInput * scrollSpeed;
            targetCameraX = Mathf.Clamp(targetCameraX, minX, maxX);
        }

        // 4. 카메라 X축 이동 보간 및 Y좌표 -1 고정
        Vector3 currentCamPos = targetCamera.transform.position;
        float newCamX = Mathf.SmoothDamp(currentCamPos.x, targetCameraX, ref currentVelocityX, smoothTime);
        targetCamera.transform.position = new Vector3(newCamX, fixedY, currentCamPos.z);

        // 5. 배경 오브젝트의 위치를 카메라와 동기화 (X는 연동, Y는 -1 고정, Z는 배경 고유값 유지)
        UpdateBackgroundPosition();
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        isDragging = true;
        dragOriginWorld = GetMouseWorldPosition();
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 currentMouseWorld = GetMouseWorldPosition();
        float differenceX = (dragOriginWorld.x - currentMouseWorld.x) * dragSpeed;

        targetCameraX += differenceX;
        targetCameraX = Mathf.Clamp(targetCameraX, minX, maxX);

        dragOriginWorld = GetMouseWorldPosition();
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    /// <summary>
    /// 배경 오브젝트의 XY 좌표를 카메라와 동일하게 (X는 카메라X, Y는 fixedY) 유지
    /// </summary>
    private void UpdateBackgroundPosition()
    {
        Vector3 camPos = targetCamera.transform.position;
        transform.position = new Vector3(camPos.x, fixedY, transform.position.z);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
        return targetCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    public void CalculateMaxX()
    {
        if (StageManager.Instance != null && StageManager.Instance.floors.Count > 0)
        {
            var lastFloor = StageManager.Instance.floors[StageManager.Instance.floors.Count - 1];
            if (lastFloor.Count > 0)
            {
                float lastFloorIndex = lastFloor[0].floor;
                float spacingX = 3.5f;

                maxX = Mathf.Max(minX, lastFloorIndex * spacingX);
                return;
            }
        }

        maxX = 35f;
    }
}