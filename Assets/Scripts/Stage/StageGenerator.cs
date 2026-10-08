using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageGenerator : MonoBehaviour
{
    public List<List<StageNode>> GenerateMap(int width, int height, Dictionary<StageType, float> weightTable)
    {
        int nodeIdSequence = 0;
        List<List<StageNode>> floors = CreateGridNodes(width, height, ref nodeIdSequence);
        ConnectStagePaths(floors, height);
        RemoveIsolatedNodes(floors, height);
        AssignStageTypes(floors, height, weightTable);
        return floors;
    }

    private List<List<StageNode>> CreateGridNodes(int width, int height, ref int idSeq)
    {
        List<List<StageNode>> floors = new List<List<StageNode>>();

        for (int f = 0; f < height; f++)
        {
            List<StageNode> currentFloorNodes = new List<StageNode>();
            int nodeCount = (f == 0 || f == height - 1) ? 1 : UnityEngine.Random.Range(2, width + 1);

            List<int> availableX = Enumerable.Range(0, width).ToList();
            ShuffleList(availableX);

            for (int i = 0; i < nodeCount; i++)
            {
                int chosenX = (f == 0 || f == height - 1) ? (width / 2) : availableX[i];
                StageNode node = new StageNode(idSeq++, f, chosenX);

                // 1층 노드는 최초 진입 가능
                if (f == 1) node.canGo = true;

                currentFloorNodes.Add(node);
            }

            currentFloorNodes = currentFloorNodes.OrderBy(n => n.xIndex).ToList();
            floors.Add(currentFloorNodes);
        }

        return floors;
    }

    private void ConnectStagePaths(List<List<StageNode>> floors, int height)
    {
        for (int f = 0; f <= height - 2; f++)
        {
            List<StageNode> currentFloor = floors[f];
            List<StageNode> nextFloor = floors[f + 1];

            if (f == 0)
            {
                foreach (var nextNode in nextFloor) LinkNodes(currentFloor[0], nextNode);
                continue;
            }

            if (f == height - 2)
            {
                StageNode bossNode = nextFloor[0];
                foreach (var node in currentFloor) LinkNodes(node, bossNode);
                continue;
            }

            int minNextIndexToConnect = 0;

            for (int i = 0; i < currentFloor.Count; i++)
            {
                StageNode curr = currentFloor[i];

                var validCandidates = nextFloor
                    .Where((next, idx) => idx >= minNextIndexToConnect && Mathf.Abs(next.xIndex - curr.xIndex) <= 1)
                    .ToList();

                if (validCandidates.Count == 0)
                {
                    var nearest = nextFloor
                        .Where((next, idx) => idx >= minNextIndexToConnect)
                        .OrderBy(next => Mathf.Abs(next.xIndex - curr.xIndex))
                        .FirstOrDefault() ?? nextFloor.Last();

                    validCandidates.Add(nearest);
                }

                int connectCount = Mathf.Min(UnityEngine.Random.Range(1, 3), validCandidates.Count);
                for (int c = 0; c < connectCount; c++)
                {
                    LinkNodes(curr, validCandidates[c]);
                }

                int connectedMaxIdx = nextFloor.IndexOf(validCandidates[connectCount - 1]);
                minNextIndexToConnect = connectedMaxIdx;
            }

            foreach (var next in nextFloor)
            {
                if (next.prevStages.Count == 0)
                {
                    var nearestPrev = currentFloor.OrderBy(c => Mathf.Abs(c.xIndex - next.xIndex)).First();
                    LinkNodes(nearestPrev, next);
                }
            }
        }
    }

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
                    foreach (var prev in node.prevStages) prev.nextStages.Remove(node);
                    foreach (var next in node.nextStages) next.prevStages.Remove(node);
                    currentFloor.RemoveAt(i);
                }
            }
        }
    }

    private void AssignStageTypes(List<List<StageNode>> floors, int height, Dictionary<StageType, float> weightTable)
    {
        ProbabilityCorrector<StageType> corrector = new ProbabilityCorrector<StageType>(weightTable);

        for (int f = 0; f < height; f++)
        {
            foreach (var node in floors[f])
            {
                if (f == 0)
                {
                    node.stageType = StageType.Normal;
                }
                else if (f == height - 2)
                {
                    node.stageType = StageType.Rest; // 정비
                }
                else if (f == height - 1)
                {
                    node.stageType = StageType.Boss; // 보스
                }
                else
                {
                    StageType rolledType = corrector.EvaluateNext();

                    // 연속 방지: Normal 이외의 특수 노드가 직전에 동일하게 존재하면 Normal로 변경
                    if (rolledType != StageType.Normal)
                    {
                        if (node.prevStages.Any(prev => prev.stageType == rolledType))
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