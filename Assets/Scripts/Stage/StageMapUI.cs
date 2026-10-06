using System.Collections.Generic;
using UnityEngine;

public class StageMapUI : MonoBehaviour
{
    [Header("프리팹 및 컨테이너")]
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Transform mapContentContainer;
    [SerializeField] private LineRenderer lineRendererPrefab;

    [Header("가로형 배치 간격 설정")]
    [SerializeField] private float floorSpacingX = 180f; // 층간 거리 (오른쪽으로 나아가는 간격)
    [SerializeField] private float laneSpacingY = 120f;  // 격자 세로 폭 (중앙 기준 위아래 간격)

    private Dictionary<StageNode, StageNodeUI> nodeUIMap = new Dictionary<StageNode, StageNodeUI>();

    private void Start()
    {
        if (StageManager.Instance != null && StageManager.Instance.floors.Count > 0)
        {
            RenderMap(StageManager.Instance.floors);
        }
    }

    /// <summary>
    /// floors 리스트를 순회하여 UI 노드들을 격자 좌표에 인스턴스화하고 경로 라인을 생성합니다.
    /// </summary>
    public void RenderMap(List<List<StageNode>> floors)
    {
        nodeUIMap.Clear();

        for (int f = 0; f < floors.Count; f++)
        {
            foreach (var node in floors[f])
            {
                GameObject obj = Instantiate(nodePrefab, mapContentContainer);
                RectTransform rt = obj.GetComponent<RectTransform>();

                // 1. X좌표: 층수(0층 -> 1층 -> 보스층)에 따라 오른쪽(+)으로 전진
                float posX = node.floor * floorSpacingX;

                // 2. Y좌표: 레인 번호(xIndex)에 따라 중앙을 기준으로 위/아래 배치
                float posY = (node.xIndex - (StageManager.Instance.mapWidth / 2f)) * laneSpacingY;

                rt.anchoredPosition = new Vector2(posX, posY);

                StageNodeUI ui = obj.GetComponent<StageNodeUI>();
                ui.Setup(node);

                nodeUIMap.Add(node, ui);
            }
        }

        // 선 그리기 (LineRenderer는 UI 월드 포지션을 그대로 가져오므로 수정 불필요)
        DrawAllConnections(floors);
    }

    /// <summary>
    /// 노드 간 연결 경로를 LineRenderer로 생성합니다.
    /// 스크롤 시 선이 함께 움직이도록 mapContentContainer의 자식으로 붙이고 로컬 좌표를 사용합니다.
    /// </summary>
    private void DrawAllConnections(List<List<StageNode>> floors)
    {
        for (int f = 0; f < floors.Count - 1; f++)
        {
            foreach (var fromNode in floors[f])
            {
                if (!nodeUIMap.ContainsKey(fromNode)) continue;
                RectTransform startRt = nodeUIMap[fromNode].GetComponent<RectTransform>();

                foreach (var toNode in fromNode.nextStages)
                {
                    if (!nodeUIMap.ContainsKey(toNode)) continue;
                    RectTransform endRt = nodeUIMap[toNode].GetComponent<RectTransform>();

                    // 1. 선 프리팹을 스크롤되는 mapContentContainer의 자식으로 생성
                    LineRenderer line = Instantiate(lineRendererPrefab, mapContentContainer);

                    // 2. UI 버튼보다 선이 뒤로 가도록 계층 순서 맨 위(뒤쪽)로 정렬
                    line.transform.SetAsFirstSibling();

                    // 3. 로컬 좌표계 사용 설정 (스크롤 뷰 드래그 시 선이 함께 이동)
                    line.useWorldSpace = false;
                    line.positionCount = 2;

                    // 4. Content 기준 localPosition을 가져오고, Z축을 살짝 뒤로 밀어 버튼 뒤에 배치
                    Vector3 startPos = startRt.localPosition;
                    Vector3 endPos = endRt.localPosition;

                    // UI 캔버스 평면(Z=0)보다 살짝 뒤로 밀기 (카메라 Forward 방향 기준)
                    startPos.z = 1f;
                    endPos.z = 1f;

                    line.SetPosition(0, startPos);
                    line.SetPosition(1, endPos);
                }
            }
        }
    }
}