using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 타입 정의 (기획서 4-1 데이터 테이블 기준)
/// </summary>
public enum StageType
{
    Normal,     // 기본 전투
    Elite,      // 엘리트 전투
    Upgrade,    // 카드 강화
    DeleteHeal, // 카드 삭제 및 체력 회복
    Boss        // 보스 전투
}

/// <summary>
/// 개별 스테이지 노드의 런타임 데이터 구조체
/// </summary>
[Serializable]
public class StageNode
{
    public int id;
    public int floor;           // y좌표 (층)
    public int xIndex;          // x좌표 (0 ~ width-1)
    public StageType stageType; // 배정된 스테이지 타입
    public bool canGo;          // 플레이어 진입 가능 여부
    public bool isVisited;      // 이미 방문한 스테이지인지 여부

    // 노드 간 연결 참조 (순환 직렬화 방지를 위해 NonSerialized 처리)
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