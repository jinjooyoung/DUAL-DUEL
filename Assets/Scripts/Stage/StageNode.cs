using System;
using System.Collections.Generic;

public enum StageType
{
    Normal, // 일반 전투
    Elite,  // 엘리트 전투
    Boss,   // 보스 전투
    Rest,   // 정비 (회복, 강화, 삭제)
    Shop,   // 상점
    Random  // 랜덤 (노멀~상점 중 입장 시 결정)
}

[Serializable]
public class StageNode
{
    public int id;
    public int floor;           // y 좌표 (층)
    public int xIndex;          // 레인 인덱스
    public StageType stageType; // 노드 타입
    public bool canGo;          // 진입 가능 여부
    public bool isVisited;      // 방문 여부

    [NonSerialized] public List<StageNode> prevStages = new List<StageNode>();
    [NonSerialized] public List<StageNode> nextStages = new List<StageNode>();

    public StageNode(int id, int floor, int xIndex)
    {
        this.id = id;
        this.floor = floor;
        this.xIndex = xIndex;
        this.stageType = StageType.Normal;
        this.canGo = false;
        this.isVisited = false;
        this.prevStages = new List<StageNode>();
        this.nextStages = new List<StageNode>();
    }
}