using System;
using System.Collections.Generic;

/// <summary>
/// 저장 파일 한 개에 들어가는 런 데이터. JsonUtility 로 직렬화되도록 모두 int / bool / float 배열과 리스트로만 구성.
/// </summary>
[Serializable]
public class SaveData
{
    public int version = SaveManager.Version;

    // 플레이어
    public List<CardSaveData> deck = new List<CardSaveData>();
    public int currentHP;
    public int currentGold;

    // 가중치 추첨기: [키1, 값1, 키2, 값2 ...] (키 = RankType 정수값)
    public float[] rewardWeights;
    public float[] shopWeights;      // 상점에 한 번도 안 갔으면 비어 있음

    // 스테이지
    public int currentChapter;
    public List<FloorSaveData> floors = new List<FloorSaveData>();
}

/// <summary>
/// 카드 한 장: CardSO 아이디 + 강화 단계
/// </summary>
[Serializable]
public class CardSaveData
{
    public int cardId;
    public int upgradeLevel;
}

/// <summary>
/// 한 층의 노드들 (JsonUtility 는 List 안의 List 를 저장하지 못해서 층마다 이 클래스로 감쌈)
/// </summary>
[Serializable]
public class FloorSaveData
{
    public List<NodeSaveData> nodes = new List<NodeSaveData>();
}

/// <summary>
/// 노드 하나: StageNode 의 값 + 연결된 이전/다음 노드의 id
/// </summary>
[Serializable]
public class NodeSaveData
{
    public int id;
    public int floor;
    public int xIndex;
    public int stageType;    // (int)StageType
    public bool canGo;
    public bool isVisited;
    public int[] prevIds;
    public int[] nextIds;
}
