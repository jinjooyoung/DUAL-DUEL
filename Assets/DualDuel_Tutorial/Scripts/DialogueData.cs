using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엑셀 Dialogue 시트 한 행을 그대로 받는 데이터 (열 순서 = 엑셀 Dialogue 시트 열 순서)
/// </summary>
[Serializable]
public class DialogueData
{
    public int id;
    public string characterName;    // 비우면 이름표를 숨김
    public string text;             // 셀 안의 \n 은 줄바꿈으로 표시됨
    public int nextId;              // 다음 대사 id. -1 이면 여기서 대사창이 닫히고 이 묶음이 끝남
    public string portraitPath;     // Resources 기준 경로 (예: Tutorial/Guide). 비우면 스탠딩 없음
    public string expression;       // 표정 (예: Smile -> Resources/Tutorial/Guide_Smile 를 먼저 찾음)

    public const int END = -1;
}

/// <summary>
/// 대사 테이블. 엑셀(Dialogue 시트) -> CSV -> Tools > DUAL DUEL > 대사 CSV 가져오기 로 채우거나,
/// 기존 엑셀 임포터에서 SetLines 로 채워도 된다.
/// </summary>
[CreateAssetMenu(fileName = "DialogueTable", menuName = "SO/DataSO/DialogueTable")]
public class DialogueTableSO : ScriptableObject
{
    public List<DialogueData> lines = new List<DialogueData>();

    private Dictionary<int, DialogueData> map;

    /// <summary>
    /// 외부 임포터에서 대사 목록을 통째로 교체할 때 사용
    /// </summary>
    public void SetLines(List<DialogueData> newLines)
    {
        lines = newLines ?? new List<DialogueData>();
        map = null;
    }

    /// <summary>
    /// id 로 대사 찾기
    /// </summary>
    public bool TryGet(int id, out DialogueData line)
    {
        if (map == null) BuildMap();
        return map.TryGetValue(id, out line);
    }

    // id -> 대사 사전 (중복 id 는 경고하고 먼저 나온 행을 사용)
    private void BuildMap()
    {
        map = new Dictionary<int, DialogueData>();
        foreach (DialogueData line in lines)
        {
            if (line == null) continue;
            if (map.ContainsKey(line.id))
            {
                Debug.LogWarning($"[DialogueTable] 중복된 대사 id {line.id} 는 먼저 나온 행만 사용합니다.");
                continue;
            }
            map.Add(line.id, line);
        }
    }

    // 인스펙터에서 값을 바꾸면 사전을 다시 만들도록
    private void OnValidate() => map = null;
}
