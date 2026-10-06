using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageGenerator : MonoBehaviour
{
    /// <summary>
    /// 기획서 4단계 프로세스를 순차 실행하여 완성된 스테이지 그래프 맵(floors)을 반환합니다.
    /// </summary>
    /// <param name="width">층당 최대 x 격자 폭 (5)</param>
    /// <param name="height">총 층수 (10층 -> 0층 시작, 8층 휴식, 9층 보스)</param>
    /// <param name="weightTable">중간 층에서 사용할 가중치 테이블</param>
    /// <returns>층별로 정렬된 List<List<StageNode>></returns>
    public List<List<StageNode>> GenerateMap(int width, int height, Dictionary<StageType, float> weightTable)
    {
        int nodeIdSequence = 0;

        // 1단계: 격자 노드 생성
        List<List<StageNode>> floors = CreateGridNodes(width, height, ref nodeIdSequence);

        // 2단계: 상위 층 근접 노드 연결
        ConnectStagePaths(floors, height);

        // 3단계: 전/후 연결이 없는 고립 노드 제거
        RemoveIsolatedNodes(floors, height);

        // 4단계: ProbabilityCorrector 기반 타입 배정 및 연속 타입 방지 보정
        AssignStageTypes(floors, height, weightTable);

        return floors;
    }

    /// <summary>
    /// [1단계] 층별 노드 인스턴스를 격자 좌표 기반으로 생성합니다.
    /// 0층(시작) 1개, 최상층(보스) 1개, 나머지 층은 2~width개 무작위 배치.
    /// </summary>
    private List<List<StageNode>> CreateGridNodes(int width, int height, ref int idSeq)
    {
        List<List<StageNode>> floors = new List<List<StageNode>>();

        for (int f = 0; f < height; f++)
        {
            List<StageNode> currentFloorNodes = new List<StageNode>();
            int nodeCount = 0;

            if (f == 0 || f == height - 1)
            {
                nodeCount = 1; // 0층과 최상층은 무조건 1개
            }
            else
            {
                nodeCount = UnityEngine.Random.Range(2, width + 1); // 2 이상 width 이하
            }

            // x좌표 중복 없이 랜덤 선택
            List<int> availableX = Enumerable.Range(0, width).ToList();
            ShuffleList(availableX);

            for (int i = 0; i < nodeCount; i++)
            {
                int chosenX = (f == 0 || f == height - 1) ? (width / 2) : availableX[i];
                StageNode node = new StageNode(idSeq++, f, chosenX);

                // 1층 노드만 최초 진입 가능(canGo = true) 처리
                if (f == 1) node.canGo = true;

                currentFloorNodes.Add(node);
            }

            // xIndex 기준 오름차순 정렬
            currentFloorNodes = currentFloorNodes.OrderBy(n => n.xIndex).ToList();
            floors.Add(currentFloorNodes);
        }

        return floors;
    }

    /// <summary>
    /// [2단계] 하위 층 노드에서 상위 층 노드로 1~3개 경로를 x좌표 거리 기준으로 연결합니다.
    /// </summary>
    private void ConnectStagePaths(List<List<StageNode>> floors, int height)
    {
        for (int f = 0; f <= height - 2; f++)
        {
            List<StageNode> currentFloor = floors[f];
            List<StageNode> nextFloor = floors[f + 1];

            foreach (var node in currentFloor)
            {
                // 0층이거나 보스 직전 층(height - 2)은 윗층 전체 노드와 연결
                if (f == 0 || f == height - 2)
                {
                    foreach (var nextNode in nextFloor)
                    {
                        LinkNodes(node, nextNode);
                    }
                    continue;
                }

                // x좌표 거리 기준으로 윗층 노드 오름차순 정렬
                var sortedNextNodes = nextFloor.OrderBy(next => Mathf.Abs(next.xIndex - node.xIndex)).ToList();

                // 1~3개 랜덤 선택 (윗층 총 노드 개수 한도 내)
                int connectCount = UnityEngine.Random.Range(1, 4);
                connectCount = Mathf.Min(connectCount, sortedNextNodes.Count);

                for (int i = 0; i < connectCount; i++)
                {
                    LinkNodes(node, sortedNextNodes[i]);
                }
            }
        }
    }

    /// <summary>
    /// [3단계] 1층부터 height-2층까지 prev 또는 next가 비어있는 고립 노드를 제거합니다.
    /// </summary>
    private void RemoveIsolatedNodes(List<List<StageNode>> floors, int height)
    {
        for (int f = 1; f <= height - 2; f++)
        {
            List<StageNode> currentFloor = floors[f];

            for (int i = currentFloor.Count - 1; i >= 0; i--)
            {
                StageNode node = currentFloor[i];

                if (node.prevStages.Count == 0 || node.nextStages.Count == 0)
                {
                    // 연결된 다른 노드의 참조도 함께 정리
                    foreach (var prev in node.prevStages) prev.nextStages.Remove(node);
                    foreach (var next in node.nextStages) next.prevStages.Remove(node);

                    currentFloor.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// [4단계] 층 규칙(0층:Normal, H-2층:DeleteHeal, H-1층:Boss) 및
    /// ProbabilityCorrector를 사용하여 타입을 배정하고, prevStages 연속 동일 타입을 차단합니다.
    /// </summary>
    private void AssignStageTypes(List<List<StageNode>> floors, int height, Dictionary<StageType, float> weightTable)
    {
        ProbabilityCorrector<StageType> corrector = new ProbabilityCorrector<StageType>(weightTable);

        for (int f = 0; f < height; f++)
        {
            foreach (var node in floors[f])
            {
                // 특수 층 고정 배정 규칙
                if (f == 0)
                {
                    node.stageType = StageType.Normal;
                }
                else if (f == height - 2)
                {
                    node.stageType = StageType.DeleteHeal; // 휴식/삭제
                }
                else if (f == height - 1)
                {
                    node.stageType = StageType.Boss;
                }
                else
                {
                    // 중간 일반 층: 동적 확률 보정 룰렛 호출
                    StageType rolledType = corrector.EvaluateNext();

                    // 연속 방지 규칙: Normal이 아닌 특수 노드가 이전 노드에 동일하게 존재한다면 강제로 Normal 변경
                    if (rolledType != StageType.Normal)
                    {
                        bool hasSameInPrev = node.prevStages.Any(prev => prev.stageType == rolledType);
                        if (hasSameInPrev)
                        {
                            rolledType = StageType.Normal;
                        }
                    }

                    node.stageType = rolledType;
                }
            }
        }
    }

    private void LinkNodes(StageNode from, StageNode to)
    {
        if (!from.nextStages.Contains(to)) from.nextStages.Add(to);
        if (!to.prevStages.Contains(from)) to.prevStages.Add(from);
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}