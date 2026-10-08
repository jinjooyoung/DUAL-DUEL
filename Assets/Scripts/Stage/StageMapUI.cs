using System.Collections.Generic;
using UnityEngine;

public class StageMapUI : MonoBehaviour
{
    [Header("프리팹 및 컨테이너")]
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Transform mapContainer;
    [SerializeField] private LineRenderer linePrefab;

    [Header("3D 월드 간격 설정")]
    [SerializeField] private float floorSpacingX = 3.5f;
    [SerializeField] private float laneSpacingY = 2.0f;

    private Dictionary<StageNode, StageNodeUI> nodeUIMap = new Dictionary<StageNode, StageNodeUI>();

    public void RenderMap(List<List<StageNode>> floors)
    {
        if (mapContainer == null) mapContainer = transform;

        for (int i = mapContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(mapContainer.GetChild(i).gameObject);
        }
        nodeUIMap.Clear();

        // 1. 노드 월드 좌표 배치
        for (int f = 0; f < floors.Count; f++)
        {
            /* [5번 요구사항: 0층 노드 비노출 처리]
            if (f == 0)
            {
                // 0층(최하층) 노드는 화면에 표시하지 않음
                continue;
            }
            */

            foreach (var node in floors[f])
            {
                // 0층 노드 생성 주석 처리
                if (f == 0) continue;

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
            /* [5번 요구사항: 0층에서 뻗어나가는 라인렌더러 비노출 처리]
            if (f == 0)
            {
                continue;
            }
            */
            if (f == 0) continue;

            foreach (var fromNode in floors[f])
            {
                if (!nodeUIMap.ContainsKey(fromNode)) continue;
                Vector3 startPos = nodeUIMap[fromNode].transform.position;

                foreach (var toNode in fromNode.nextStages)
                {
                    if (!nodeUIMap.ContainsKey(toNode)) continue;
                    Vector3 endPos = nodeUIMap[toNode].transform.position;

                    LineRenderer line = Instantiate(linePrefab, mapContainer);
                    line.useWorldSpace = true;
                    line.positionCount = 2;

                    startPos.z = 0.5f;
                    endPos.z = 0.5f;

                    line.SetPosition(0, startPos);
                    line.SetPosition(1, endPos);
                }
            }
        }
    }
}