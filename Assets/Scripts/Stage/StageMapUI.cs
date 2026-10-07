using System.Collections.Generic;
using UnityEngine;

public class StageMapUI : MonoBehaviour
{
    [Header("프리팹 및 컨테이너")]
    [SerializeField] private GameObject nodePrefab;       // 3D/Sprite 노드 프리팹
    [SerializeField] private Transform mapContainer;      // 맵이 생성될 부모 Transform
    [SerializeField] private LineRenderer linePrefab;     // LineRenderer 프리팹

    [Header("3D 월드 간격 설정")]
    [SerializeField] private float floorSpacingX = 3.5f;  // 층간 X 간격 (월드 유닛 단위)
    [SerializeField] private float laneSpacingY = 2.0f;   // 세로 레인 Y 간격 (월드 유닛 단위)

    private Dictionary<StageNode, StageNodeUI> nodeUIMap = new Dictionary<StageNode, StageNodeUI>();

    public void RenderMap(List<List<StageNode>> floors)
    {
        if (mapContainer == null) mapContainer = transform;

        // 기존 생성 오브젝트 제거
        for (int i = mapContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(mapContainer.GetChild(i).gameObject);
        }
        nodeUIMap.Clear();

        // 1. 노드 월드 좌표 배치
        for (int f = 0; f < floors.Count; f++)
        {
            foreach (var node in floors[f])
            {
                float posX = node.floor * floorSpacingX;
                float posY = (node.xIndex - (StageManager.Instance.mapWidth / 2f)) * laneSpacingY;
                Vector3 worldPos = new Vector3(posX, posY, 0f);

                GameObject obj = Instantiate(nodePrefab, worldPos, Quaternion.identity, mapContainer);
                StageNodeUI ui = obj.GetComponent<StageNodeUI>();
                if (ui != null)
                {
                    ui.Setup(node);
                    nodeUIMap.Add(node, ui);
                }
            }
        }

        // 2. 월드 좌표 기준 선 연결
        DrawAllConnections(floors);
    }

    private void DrawAllConnections(List<List<StageNode>> floors)
    {
        for (int f = 0; f < floors.Count - 1; f++)
        {
            foreach (var fromNode in floors[f])
            {
                if (!nodeUIMap.ContainsKey(fromNode)) continue;
                Vector3 startPos = nodeUIMap[fromNode].transform.position;

                foreach (var toNode in fromNode.nextStages)
                {
                    if (!nodeUIMap.ContainsKey(toNode)) continue;
                    Vector3 endPos = nodeUIMap[toNode].transform.position;

                    LineRenderer line = Instantiate(linePrefab, mapContainer);
                    line.useWorldSpace = true; // 월드 좌표 직접 적용
                    line.positionCount = 2;

                    // 선을 노드 메쉬보다 살짝 뒤로 밀어 Z축 정렬 (+Z 방향)
                    startPos.z = 0.5f;
                    endPos.z = 0.5f;

                    line.SetPosition(0, startPos);
                    line.SetPosition(1, endPos);
                }
            }
        }
    }
}